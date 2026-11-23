using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedRecordEffectsChecks
{
    internal const string TestName = "compiler/beam/projects-retained-record-effects-at-the-point-of-use";
    private const string Fixture = "beam_retained_record_effects.erl";
    private const string WrongProjection = "A record-field effect projected its state before allocating the retained frame.";
    private const string WrongLifetime = "A shared record field did not release its slot before the next effect.";
    private const string WrongFallback = "A record validation failure lost its source-specific fallback.";
    private const string WrongBinding = "A repeated source binding was removed from the record match.";

    private const string Source = """
        -module(beam_retained_record_effects).
        -export([plain/3,record_report/3,port_report/3,event_report/2]).
        -record(state,{parent,config,manager,timer}).
        -record(wrapper,{event}).
        -record(cell,{kind,row,column}).

        plain(close,_Tag,State=#state{parent=Target}) ->
            owned_effects:dispose(Target),
            {stop,normal,ok,State};
        plain(_Msg,_Tag,State) ->
            {reply,unknown,State}.

        record_report(close,_Tag,State=#state{parent=Target,manager=Manager}) ->
            owned_effects:prepare(Manager),
            owned_effects:dispose(Manager),
            owned_effects:dispose(Target),
            {stop,normal,ok,State};
        record_report(Msg,_Tag,State) ->
            owned_effects:report(State#state.config,"notice ~p",[Msg]),
            {reply,{unknown,request},State}.

        port_report(close,_Tag,State=#state{parent=_Target}) ->
            owned_effects:dispose(State#state.timer),
            {stop,normal,ok,State};
        port_report(Msg,_Tag,State) ->
            owned_effects:report("notice ~p",[Msg]),
            {reply,unknown,State}.

        event_report(#wrapper{event=#cell{kind=modified,row=Row,column=Col}},State=#state{}) ->
            Value=owned_effects:read(State#state.parent,Row,Col),
            owned_effects:report(State#state.config,"notice ~p",[Row,Col,Value]),
            {noreply,State}.
        """;

    internal static Task Run()
    {
        var functions = ReadFunctions(Source);
        var body = functions("plain");
        int allocation = Find(body, Allocate);
        if (allocation < 0 || allocation > Find(body, GetTupleElement)
            || Value(Operands(body[allocation])[0]) != 1 || Value(Operands(body[allocation])[1]) != 3)
        {
            throw new InvalidOperationException(WrongProjection);
        }

        var shared = functions("record_report");
        int trim = Find(shared, TrimFrame);
        if (Find(shared, GetTupleElement) > trim || trim < 0 || Value(Operands(shared[trim])[0]) != 1)
        {
            throw new InvalidOperationException(WrongLifetime);
        }

        if (Find(shared, BadRecord) < 0 || Find(functions("port_report"), BadRecord) >= 0)
        {
            throw new InvalidOperationException(WrongFallback);
        }

        string repeated = Source.Replace("parent=Target,manager=Manager", "parent=Target,manager=Target", StringComparison.Ordinal)
            .Replace("prepare(Manager)", "prepare(Target)", StringComparison.Ordinal)
            .Replace("dispose(Manager)", "dispose(Target)", StringComparison.Ordinal);
        if (!ReadFunctions(repeated)("record_report").Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1).All(operand => (string)Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        var read = functions("event_report");
        int eventTrim = Find(read, TrimFrame);
        if (eventTrim < 0 || Value(Operands(read[eventTrim])[0]) != 2)
        {
            throw new InvalidOperationException(WrongLifetime);
        }

        string repeatedEvent = Source.Replace("row=Row,column=Col", "row=Row,column=Row", StringComparison.Ordinal)
            .Replace("Row,Col", "Row,Row", StringComparison.Ordinal);
        if (!ReadFunctions(repeatedEvent)("event_report").Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1).All(operand => (string)Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static Func<string, object[]> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return name =>
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
            );
            if (info < 0)
            {
                throw new InvalidOperationException(Fixture);
            }

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static int Find(object[] instructions, byte opcode) =>
        Array.FindIndex(instructions, instruction => Opcode(instruction) == opcode);

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

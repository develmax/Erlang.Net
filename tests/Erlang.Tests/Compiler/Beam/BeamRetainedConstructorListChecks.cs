using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedConstructorListChecks
{
    internal const string TestName = "compiler/beam/retains-owned-constructor-lists-and-selects-record-events";
    private const string Fixture = "beam_retained_constructor_list.erl";
    private const string WrongFrame = "The constructor list did not share the parent slot and trim to its retained owner.";
    private const string WrongCapture = "The callback did not keep both existing lexical captures.";
    private const string WrongDispatch = "The nested records did not share their selector and preserve the case failure.";
    private const string WrongBinding = "The event dispatch lost a repeated variable match.";
    private const string WrongOrder = "The standalone comprehension was not emitted between its declared siblings' closures.";

    private const string OrderSource = """
        -module(beam_retained_constructor_list).
        -export([first/1,map/0,last/1]).

        first(Value) -> fun() -> Value end.
        map() ->
            Labels = ["label " ++ integer_to_list(Number) || Number <- lists:seq(1,3)],
            owned_list_effects:echo(Labels),
            Labels.
        last(Value) -> fun() -> Value end.
        """;

    private const string Source = """
        -module(beam_retained_constructor_list).
        -export([build/1,event/2]).
        -record(envelope,{object,event}).
        -record(command,{mode,payload,number}).
        -record(state,{owner,config}).

        build(Parent) ->
            Owner = owned_list_effects:new_owner(owner,Parent,[]),
            Children = [owned_list_effects:new(Parent,1,"first",[]),
                         owned_list_effects:new(Parent,2,"second",[{style,2}]),
                         owned_list_effects:new(Parent,3,"third",[]),
                         owned_list_effects:new(Parent,4,"last",[])],
            Callback = fun(Item) -> owned_list_effects:attach(Owner,Item) end,
            owned_list_effects:apply_rows(Callback,Children),
            Owner.

        event(#envelope{event=#command{mode=direct,payload=Data}}, State=#state{}) ->
            owned_list_effects:report(State#state.config,"direct",[Data]),
            {noreply,State};
        event(#envelope{object=Object,event=#command{mode=selected,number=Number}}, State=#state{config=Config}) ->
            Name = owned_list_effects:read(Object),
            case Number of
                0 -> owned_list_effects:report(Config,"zero",[Name]);
                1 -> owned_list_effects:report(Config,"one",[Name]);
                2 -> owned_list_effects:report(Config,"two",[Name])
            end,
            {noreply,State};
        event(#envelope{object=Object,event=#command{mode=report}}, State=#state{}) ->
            Name = owned_list_effects:read(Object),
            owned_list_effects:report(State#state.config,"report",[Name]),
            {noreply,State}.
        """;

    internal static Task Run()
    {
        object[] body = ReadBody(Source, "build");
        int swap = Array.FindIndex(body, instruction => Opcode(instruction) == Swap);
        int closure = Array.FindIndex(body, instruction => Opcode(instruction) == MakeFun);
        int trim = Array.FindIndex(body, instruction => Opcode(instruction) == TrimFrame);
        if (swap < 0 || swap >= closure || trim <= closure
            || Value(Operands(body[trim])[0]) != 3 || Value(Operands(body[trim])[1]) != 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        string captured = Source.Replace("attach(Owner,Item)", "attach_with_parent(Parent,Owner,Item)", StringComparison.Ordinal);
        object[] captureBody = ReadBody(captured, "build");
        var capturedClosure = captureBody.First(instruction => Opcode(instruction) == MakeFun);
        if (Value(Operands(capturedClosure)[2]) != 2)
        {
            throw new InvalidOperationException(WrongCapture);
        }

        object[] dispatch = ReadBody(Source, "event");
        if (dispatch.Count(instruction => Opcode(instruction) == SelectValue) != 2
            || !dispatch.Where(instruction => Opcode(instruction) == CaseEnd)
                .Select(Operands).Any(operands => (string)Property(operands[0], "Kind") == "y"))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        string repeated = Source.Replace("mode=direct,payload=Data", "mode=direct,payload=Data,number=Data", StringComparison.Ordinal);
        if (!ReadBody(repeated, "event").Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1).All(operand => (string)Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        var (instructions, atoms) = ReadInstructions(OrderSource);
        var functions = instructions.Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => atoms[Value(Operands(instruction)[1])]).ToArray();
        int first = Array.IndexOf(functions, "-first/1-fun-0-");
        int map = Array.IndexOf(functions, "-map/0-lc$^0/1-0-");
        int last = Array.IndexOf(functions, "-last/1-fun-0-");
        if (last < 0 || map <= last || first <= map)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;
    }

    private static object[] ReadBody(string source, string name)
    {
        var (instructions, atoms) = ReadInstructions(source);
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
        );
        if (info < 0)
        {
            throw new InvalidOperationException(Fixture);
        }

        return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static (object[] Instructions, string[] Atoms) ReadInstructions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (instructions, atoms);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

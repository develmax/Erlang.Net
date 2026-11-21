using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordFieldCallOperandChecks
{
    internal const string TestName = "compiler/beam/reuses-a-checked-record-across-projection-and-closure-capture";
    private const string Fixture = "beam_record_field_call_operands.erl";
    private const string WrongProjection = "The two projections did not share the checked record and its bounded call frame.";
    private const string WrongCapture = "The record proof did not distinguish checked captures from arbitrary input.";
    private const string WrongMoves = "The independent tail argument move did not precede the forwarding chain.";

    private const string Source = """
        -module(beam_record_field_call_operands).
        -export([handle/2, unchecked/2, invoke/1]).
        -record(sample_state, {consumers}).
        -record(sample_event, {name, data}).

        handle(Event, State=#sample_state{consumers=Receivers}) ->
            record_call_probe:before(entered),
            record_call_probe:fields(fields, [Event#sample_event.name, Event#sample_event.data]),
            lists:foreach(fun(Receiver) -> deliver(Receiver, Event) end, Receivers),
            {ok,State}.

        deliver({receiver,Target}, Event=#sample_event{}) ->
            record_call_probe:delivered(Target, Event).

        unchecked(Event, Receivers) ->
            lists:foreach(fun(Receiver) -> deliver_unchecked(Receiver, Event) end, Receivers).

        deliver_unchecked({receiver,Target}, Event=#sample_event{}) ->
            record_call_probe:delivered(Target, Event).

        invoke(Value) ->
            record_call_probe:triple(first, second, Value).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        var handler = Operations("handle");
        var frame = handler.FirstOrDefault(instruction => Opcode(instruction) == Allocate);
        if (frame is null || Value(Operands(frame)[0]) != 2
            || handler.Count(instruction => TestsRecord(instruction, "sample_event")) != 1
            || handler.Count(instruction => Opcode(instruction) == BadRecord) != 1)
        {
            throw new InvalidOperationException(WrongProjection);
        }

        if (Operations("deliver").Any(instruction => TestsRecord(instruction, "sample_event"))
            || !Operations("deliver_unchecked").Any(instruction => TestsRecord(instruction, "sample_event")))
        {
            throw new InvalidOperationException(WrongCapture);
        }

        object[] moves = Operations("invoke").Where(instruction => Opcode(instruction) == Move).ToArray();
        if (moves is not [var separate, var forwarded, var overwritten]
            || !MovesTo(separate, 1) || !MovesTo(forwarded, 2) || !MovesTo(overwritten, 0))
        {
            throw new InvalidOperationException(WrongMoves);
        }

        return Task.CompletedTask;

        bool TestsRecord(object instruction, string tag) => Opcode(instruction) == IsTaggedTuple
            && atoms[Value(Operands(instruction)[3])] == tag;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(WrongProjection);
        }
    }

    private static bool MovesTo(object instruction, int register) =>
        (string)Property(Operands(instruction)[1], "Kind") == BeamBinaryHostNames.X && Value(Operands(instruction)[1]) == register;

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

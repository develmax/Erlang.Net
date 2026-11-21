using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTupleContinuationChecks
{
    internal const string TestName = "compiler/beam/shares-a-tagged-guard-and-reuses-its-dead-slot";
    private const string Fixture = "beam_guarded_tuple_continuation_probe.erl";
    private const string WrongGuards = "The closed decoder must share one typed tuple test and one projection across both guard outcomes.";
    private const string WrongOpenGuards = "A decoder with another tuple tag must retain its tagged test.";
    private const string WrongLayout = "Nested tagged alternatives must share their projections and number branch labels in emission order.";
    private const string WrongFrames = "The two guarded tuple arms must enter independent four-slot and two-slot frames.";

    private const string Source = """
        -module(beam_guarded_tuple_continuation_probe).
        -export([route/4, pure/4, overlap/4]).

        route({event,{ready,Value,Num},Rest}, Context, Previous, _) ->
            case decode(Value) of
                {ok,Item} when Rest == [] -> lists:reverse([Item|Previous]);
                {ok,Captured} -> advance(scan(Rest), Num+1, [Captured|Previous], Rest);
                _ -> {error,{Context,Num}}
            end;
        route({event,{failed,Reason,Num},_}, Context, _, _) -> {bad,{Context,Num},Reason};
        route({event,{end_marker,Num},_}, Context, _, _) -> {stop,Context,Num};
        route({more,_}, Context, Previous, Rest) ->
            case parts(Rest) of
                [] -> lists:reverse(Previous);
                _ -> {bad,Context}
            end.

        pure({event,{ready,Value,Num},Rest}, Context, Previous, _) ->
            case decode(Value) of
                {ok,Item} when [] =:= Rest -> lists:reverse([Item|Previous]);
                {ok,Captured} -> answer(Captured,Num,Rest);
                _ -> {error,{Context,Num}}
            end;
        pure({event,{end_marker,Num},_}, Context, _, _) -> {stop,Context,Num};
        pure({more,Value}, _, _, _) -> Value.

        overlap({event,{ready,Value,Num},Rest}, Context, Previous, _) ->
            case decode(Value) of
                {ok,Item} when Rest == [] -> advance(Context,Num,[Item|Previous],Rest);
                {ok,Captured} -> advance(Context,Num,[Captured|Previous],Rest);
                _ -> {error,{Context,Num}}
            end;
        overlap({event,{end_marker,Num},_}, Context, _, _) -> {stop,Context,Num};
        overlap({more,Value}, _, _, _) -> Value.

        decode({source,Term}) -> {ok,Term};
        decode(_) -> invalid.
        scan(Rest) -> {scanned,Rest}.
        parts([]) -> [];
        parts(Other) -> [Other].
        advance(A,B,C,D) -> {A,B,C,D}.
        answer(A,B,C) -> {A,B,C}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] route = Body(code, "route");
        object[] pure = Body(code, "pure");
        int entry = DecodeEntry(code);
        if (!SharesDecodeProjection(route, entry) || !SharesDecodeProjection(pure, entry))
        {
            throw new InvalidOperationException(WrongGuards);
        }

        string openSource = Source.Replace(
            "decode(_) -> invalid.",
            "decode({foreign,Term}) -> {foreign,Term};\ndecode(_) -> invalid.",
            StringComparison.Ordinal
        );
        object openCode = Code(CodeGeneration.CompileBeamModule(openSource, Fixture));
        int openEntry = DecodeEntry(openCode);
        if (!StartsWithTaggedTest(AfterDecode(Body(openCode, "route"), openEntry))
            || !StartsWithTaggedTest(AfterDecode(Body(openCode, "pure"), openEntry)))
        {
            throw new InvalidOperationException(WrongOpenGuards);
        }

        int[] labels = route.Where(instruction => Opcode(instruction) == Label)
            .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value")).ToArray();
        int projections = route.TakeWhile(instruction => Opcode(instruction) != SelectValue)
            .Count(instruction => Opcode(instruction) == GetTupleElement);
        if (!labels.SequenceEqual(labels.Order()) || projections != 6)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int[] frames = route.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value")).ToArray();
        if (!frames.SequenceEqual([4, 2]))
        {
            throw new InvalidOperationException(WrongFrames);
        }
    }

    private static bool SharesDecodeProjection(object[] body, int entry)
    {
        object[] continuation = AfterDecode(body, entry);
        if (continuation.Length < 3 || Opcode(continuation[0]) != IsTuple
            || Opcode(continuation[2]) != IsNil)
        {
            return false;
        }

        object predicate = Operand(continuation[0], 1);
        int failure = Value(Operand(continuation[0], 0));
        var success = continuation.TakeWhile(instruction => Opcode(instruction) != Label
            || Value(Operand(instruction, 0)) != failure);

        return Register(predicate, "x", 0)
            && Property(predicate, "TypeIndex") is int
            && ReadsPayload(continuation[1])
            && success.Count(ReadsPayload) == 1;
    }

    private static bool StartsWithTaggedTest(object[] body) => body.Length > 0 && Opcode(body[0]) == IsTaggedTuple;

    private static bool ReadsPayload(object instruction) => Opcode(instruction) == GetTupleElement
        && Register(Operand(instruction, 0), "x", 0)
        && Value(Operand(instruction, 1)) == 1;

    private static int DecodeEntry(object code) => Value(Operand(Body(code, "decode")[0], 0));

    private static object[] AfterDecode(object[] body, int entry)
    {
        int call = Array.FindIndex(
            body,
            instruction => Opcode(instruction) == LocalCall && Value(Operand(instruction, 1)) == entry
        );

        return call < 0 ? [] : body[(call + 1)..];
    }

    private static bool Register(object operand, string kind, int register) =>
        (string)Property(operand, "Kind") == kind && Value(operand) == register;

    private static object[] Body(object code, string name)
    {
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operand(instruction, 1))] == name
        );

        return start < 0
            ? []
            : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static object Operand(object instruction, int index) => Items(instruction, "Operands").ElementAt(index);

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

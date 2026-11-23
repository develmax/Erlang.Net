using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedTupleGuardProjectionChecks
{
    internal const string TestName = "compiler/beam/sinks-checked-tuple-fields-through-guards";
    private const string Fixture = "beam_guarded_projection_probe.erl";
    private const string WrongLayout = "The projected field crossed a guard with the wrong successor requirements.";
    private const string WrongTailProjection = "The guarded call must project its other argument after the guard.";
    private const string WrongResult = "Moving a projection changed the case result.";
    private const string Source = """
        -module(beam_guarded_projection_probe).
        -export([lookup/3, compared/3, second_tuple/2, port/1]).
        -record(entry, {key, value}).

        lookup(Entry, Wanted, Default) ->
            case Entry of
                {Wanted, Value} -> Value;
                _ -> Default
            end.

        compared(Entry, Minimum, Default) ->
            case Entry of
                #entry{key = Key, value = Value} when Key >= Minimum -> Value;
                _ -> Default
            end.

        second_tuple({event, Pid, _}, {Witness, _, _}) when Pid =:= Witness -> accepted;
        second_tuple(_, State) -> {ok, State}.

        port({Address, Port}) when is_integer(Port) -> address(Address, Port);
        port(Value) -> {unknown, Value}.
        address(Address, Port) -> {address, Address, Port}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayouts(beam);
        CheckGuardedTailProjection();
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term value = Term.A("value");
            Term fallback = Term.A("fallback");
            Term receiver = Term.Tuple(Term.A("event"), first, value);
            Term witness = Term.Tuple(first, second, value);
            Term rejected = Term.Tuple(second, first, value);
            Term pair = Term.Tuple(first, value);
            Term entry = Term.Tuple(Term.A("entry"), second, value);
            Term port = Term.Tuple(value, Term.I(17));
            Term wrongPort = Term.Tuple(value, fallback);
            if (!Term.Tuple(Term.A("address"), value, Term.I(17)).Equals(await module.Invoke(context, "port", port))
                || !Term.Tuple(Term.A("unknown"), wrongPort).Equals(await module.Invoke(context, "port", wrongPort))
                || !Term.Tuple(Term.A("unknown"), fallback).Equals(await module.Invoke(context, "port", fallback))
                || !Term.A("accepted").Equals(await module.Invoke(
                    context,
                    "second_tuple",
                    receiver,
                    witness
                ))
                || !Term.Tuple(Term.A("ok"), rejected).Equals(await module.Invoke(
                    context,
                    "second_tuple",
                    receiver,
                    rejected
                ))
                || !Term.Tuple(Term.A("ok"), fallback).Equals(await module.Invoke(
                    context,
                    "second_tuple",
                    receiver,
                    fallback
                ))
                || !value.Equals(await module.Invoke(
                    context,
                    "lookup",
                    pair,
                    first,
                    fallback
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "lookup",
                    pair,
                    second,
                    fallback
                ))
                || !value.Equals(await module.Invoke(
                    context,
                    "compared",
                    entry,
                    first,
                    fallback
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "compared",
                    entry,
                    value,
                    fallback
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckGuardedTailProjection()
    {
        const int shapeFailure = 3;
        const int guardFailure = 4;
        const int tailEntry = 5;
        const int labels = 6;
        byte[] prefix =
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            IsTuple, F(shapeFailure), X(0), TestArity, F(shapeFailure), X(0), U(2)
        ];
        byte[] sibling = [GetTupleElement, X(0), U(1), X(1), IsInteger, F(guardFailure), X(1)];
        byte[] scratch = [GetTupleElement, X(0), U(0), X(2)];
        byte[] early = [.. prefix, .. scratch, .. sibling, Move, X(2), X(0)];
        byte[] late = [.. prefix, .. sibling, GetTupleElement, X(0), U(0), X(0)];
        byte[] tail = [TailLocalCall, U(2), F(tailEntry), Label, U(shapeFailure), Return, Label, U(guardFailure)];
        byte[] fallback = [Return, Label, U(tailEntry), Return, EndCode];
        if (Rewrite([.. early, .. tail, .. fallback]) is not { } actual
            || !actual.AsSpan().SequenceEqual([.. late, .. tail, .. fallback])
            || Rewrite([.. early, .. tail, Move, X(2), X(0), .. fallback]) is not null
            || Rewrite([.. early, .. tail, TestHeap, U(0), U(3), .. fallback]) is not null)
        {
            throw new InvalidOperationException(WrongTailProjection);
        }

        byte[]? Rewrite(byte[] instructions)
        {
            ReadOnlyMemory<byte> code = instructions;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)DebugLine,
                labels,
                1,
                code
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                8,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                new BeamModuleCompiler("beam_guarded_tail_projection_probe"),
                "SinkTupleReadsPastGuards",
                code,
                body
            );
        }
    }

    private static void CheckLayouts(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("second_tuple");
        int guard = Array.FindIndex(positive, instruction => Opcode(instruction) == TestArity);
        int field = Array.FindIndex(positive, instruction => Opcode(instruction) == GetTupleElement);
        byte[] negative = Body("compared").Select(Opcode).ToArray();
        int compare = Array.IndexOf(negative, IsLess);
        int projectedValue = Array.LastIndexOf(negative, GetTupleElement);
        if (guard < 0 || field <= guard || projectedValue < 0 || compare <= projectedValue)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTupleJoinFieldChecks
{
    internal const string TestName = "compiler/beam/sinks-checked-tuple-fields-into-constructor-continuations";
    private const string Fixture = "beam_checked_tuple_join_fields_probe.erl";
    private const string WrongLayout = "The continuation must preserve its original carrier and use exact collector roots.";
    private const string WrongValue = "The joined constructor changed a successful result or its rejection path.";
    private const string Source = """
        -module(beam_checked_tuple_join_fields_probe).
        -export([convert/1, decline/1]).

        convert({packet, Left, Right, Meta, Kind} = Packet) ->
            Choice = case {Left, Right} of
                {Left, none} -> {first, Left};
                {none, Right} -> {second, Right};
                _ -> {both, {fields, Right, Left}}
            end,
            case Kind of
                {payload, Value} when is_list(Value); is_binary(Value) ->
                    {packet, Choice, Meta, Value};
                _ ->
                    Packet
            end.

        decline({packet, Left, Right, Meta, Kind} = Packet) ->
            Choice = case {Left, Right} of
                {Left, none} -> {first, Left};
                {none, Right} -> {second, Right};
                _ -> {both, {fields, Right, Left}}
            end,
            case Kind of
                {payload, Value} when is_list(Value); is_binary(Value) ->
                    {packet, Choice, Meta, Value};
                _ ->
                    {fallback, Packet, Meta}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == "convert"
        );
        object[] body = instructions.Skip(entry + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line))
            .ToArray();
        if (entry < 0
            || !body.Take(4).Select(Opcode).SequenceEqual(new byte[]
                {
                    IsTaggedTuple, GetTupleElement, GetTupleElement, ExactEqualTest
                })
            || !body.Where(instruction => Opcode(instruction) == TestHeap)
                .Select(instruction => Value(Items(instruction, "Operands").Last())).SequenceEqual([2, 3, 3, 3])
            || !body.TakeLast(5).Select(Opcode).SequenceEqual(new byte[]
                {
                    TestHeap, GetTupleElement, PutTuple, Return, Return
                })
            || !Items(body[^4], "Operands").Select(Value).SequenceEqual([0, 3, 0]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term packet = Term.A("packet");
            Term none = Term.A("none");
            Term meta = Term.A("meta");
            Term left = Term.I(11);
            Term right = Term.I(12);
            Term values = Term.List(Term.I(5));
            Term payload = Term.Tuple(Term.A("payload"), values);
            Term rejected = Term.Tuple(
                packet,
                left,
                right,
                meta,
                Term.Tuple(Term.A("payload"), Term.I(2))
            );
            if (!Term.Tuple(
                packet,
                Term.Tuple(Term.A("first"), left),
                meta,
                values
            ).Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.Tuple(
                        packet,
                        left,
                        none,
                        meta,
                        payload
                    )
                ))
                || !Term.Tuple(
                    packet,
                    Term.Tuple(Term.A("second"), right),
                    meta,
                    values
                ).Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.Tuple(
                        packet,
                        none,
                        right,
                        meta,
                        payload
                    )
                ))
                || !Term.Tuple(
                    packet,
                    Term.Tuple(Term.A("both"), Term.Tuple(Term.A("fields"), right, left)),
                    meta,
                    values
                )
                    .Equals(await module.Invoke(context, "convert", Term.Tuple(
                        packet,
                        left,
                        right,
                        meta,
                        payload
                    )))
                || !rejected.Equals(await module.Invoke(context, "convert", rejected))
                || !Term.Tuple(Term.A("fallback"), rejected, meta).Equals(await module.Invoke(context, "decline", rejected)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

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
        CheckCheckedArityFields();

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

    private static void CheckCheckedArityFields()
    {
        var compiler = new BeamModuleCompiler("beam_arity_fields_fixture");
        int function = (int)Call(compiler, "Intern", "choose");
        int first = (int)Call(compiler, "Intern", "one");
        int second = (int)Call(compiler, "Intern", "two");
        byte[] arities =
        [
            .. Start(
                1,
                2,
                function,
                4
            ), IsTuple, F(6), X(0), SelectTupleArity, X(0), F(6), ExtendedListOperand, U(4),
            U(3), F(3), U(4), F(4), Label, U(3)
        ];
        byte[] fields = [GetTupleElement, X(0), U(1), X(1), GetTupleElement, X(0), U(2), X(2)];
        byte[] readTag = [GetTupleElement, X(0), U(0), X(1)];
        byte[] select = [SelectValue, X(1), F(8), ExtendedListOperand, U(4), A(first), F(5), A(second), F(7)];
        byte[] firstArm = [Label, U(5), .. fields, Move, X(2), X(0), Return];
        byte[] secondArm = [Label, U(7), .. fields, Move, X(1), X(0), Return];
        byte[] defaults = [Label, U(4), Return, Label, U(6), Return, Label, U(8), Return, EndCode];
        byte[] original = [.. arities, .. readTag, .. select, .. firstArm, .. secondArm, .. defaults];
        byte[] expected =
        [
            .. arities, GetTupleElement, X(0), U(0), X(3), .. fields,
            SelectValue, X(3), F(8), ExtendedListOperand, U(4), A(first), F(5), A(second), F(7),
            Label, U(5), Move, X(2), X(0), Return, Label, U(7), Move, X(1), X(0), Return, .. defaults
        ];
        byte[] anotherArityEnters = [Label, U(4), Jump, F(5), .. defaults[3..]];
        byte[] observesTag = [.. defaults[..^2], Move, X(1), X(0), Return, EndCode];
        byte[] observesScratch = [.. defaults[..^2], Move, X(3), X(0), Return, EndCode];
        if (Apply(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply([.. arities, .. readTag, .. select, .. firstArm, .. secondArm, .. anotherArityEnters]) is not null
            || Apply([.. arities, .. readTag, .. select, .. firstArm, .. secondArm, .. observesTag]) is not null
            || Apply([.. arities, .. readTag, .. select, .. firstArm, .. secondArm, .. observesScratch]) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ShareAritySelectedTupleFields",
            (ReadOnlyMemory<byte>)bytes,
            9
        );
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

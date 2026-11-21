using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedBinaryMeasurementChecks
{
    internal const string TestName = "compiler/beam/guarded-binary-measurement";
    private const string Fixture = "beam_guarded_binary_measurement_probe.erl";
    private const string WrongFrame = "The guarded binary measurement has incorrect register roots or frame ownership.";
    private const string WrongResult = "The guarded binary measurement changed its return or failure term.";
    private const string Source = """
        -module(beam_guarded_binary_measurement_probe).
        -compile({no_auto_import,[byte_size/1]}).
        -export([open/0, call/1, first/1, shadowed/2, encode/3, decode/3, fallback_key/3, nested_tuple/3, decode_variable/3]).
        open() ->
            fun(_, Selector, _) ->
                case Selector of
                    {tag,Binary} when is_binary(Binary) -> {Binary,erlang:byte_size(Binary)}
                end
            end.
        call(Selector) ->
            Factory = open(),
            Factory(unused, Selector, unused).
        first(Selector) ->
            case Selector of
                {tag,Binary} when is_binary(Binary) -> {Binary,erlang:byte_size(Binary)}
            end.
        shadowed(_, Selector) ->
            case Selector of
                {tag,Binary} when is_binary(Binary) -> {Binary,byte_size(Binary)}
            end.
        byte_size(_) -> local_size.
        encode(Type, Value, _) ->
            case Type of
                typed ->
                    case Value of
                        {packet, Binary} when is_binary(Binary) -> {Binary, erlang:byte_size(Binary)}
                    end
            end.
        decode(Type, Value, _) ->
            case Type of
                typed ->
                    case Value of
                        Binary when is_binary(Binary) -> {packet, Binary};
                        _ -> {packet, normalize(Value)}
                    end
            end.
        fallback_key(Type, Value, _) ->
            case Type of
                typed ->
                    case Value of
                        {packet, Binary} when is_binary(Binary) -> {Binary, erlang:byte_size(Binary)}
                    end;
                _ -> dispatch
            end.
        nested_tuple(Type, Value, _) ->
            case Type of
                {typed, Key} ->
                    case Value of
                        {packet, Binary} when is_binary(Binary) -> {Key, erlang:byte_size(Binary)}
                    end
            end.
        decode_variable(Type, Value, _) ->
            case Type of
                typed ->
                    case Value of
                        Binary when is_binary(Binary) -> {packet, Binary};
                        _ -> {other, normalize(Value)}
                    end
            end.
        normalize(Integer) -> <<Integer:16>>.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term binary = new BitString(new byte[] { 0, 1, 2 });
            Term valid = Term.Tuple(Term.A("tag"), binary);
            Term invalid = Term.Tuple(Term.A("tag"), Term.A("nonbinary"));
            Term expected = Term.Tuple(binary, Term.I(3));
            if (!expected.Equals(await module.Invoke(context, "call", valid))
                || !expected.Equals(await module.Invoke(context, "first", valid))
                || !Term.Tuple(binary, Term.A("local_size")).Equals(await module.Invoke(
                    context,
                    "shadowed",
                    Nil.Value,
                    valid
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term packet = Term.Tuple(Term.A("packet"), binary);
            Term typed = Term.A("typed");
            Term invalidType = Term.A("different");
            Term invalidPacket = Term.Tuple(Term.A("packet"), Term.A("nonbinary"));
            if (!expected.Equals(await module.Invoke(
                context,
                "encode",
                typed,
                packet,
                Nil.Value
            ))
                || !packet.Equals(await module.Invoke(
                    context,
                    "decode",
                    typed,
                    binary,
                    Nil.Value
                ))
                || !Term.Tuple(Term.A("packet"), new BitString(new byte[] { 0, 1 })).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        typed,
                        Term.I(1),
                        Nil.Value
                    ))
                || !Term.A("dispatch").Equals(await module.Invoke(
                    context,
                    "fallback_key",
                    invalidType,
                    packet,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await CheckNestedFailure(
                "encode",
                invalidType,
                packet,
                invalidType
            );
            await CheckNestedFailure(
                "encode",
                typed,
                invalidPacket,
                invalidPacket
            );
            await CheckNestedFailure(
                "decode",
                invalidType,
                binary,
                invalidType
            );
            await CheckFailure("call");
            await CheckFailure("first");

            return Nil.Value;

            async Task CheckNestedFailure(
                string function,
                Term type,
                Term payload,
                Term rejected
            )
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        function,
                        type,
                        payload,
                        Nil.Value
                    );
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("case_clause"), rejected)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }

            async Task CheckFailure(string function)
            {
                try
                {
                    _ = await module.Invoke(context, function, invalid);
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("case_clause"), invalid)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Body("-open/0-fun-0-");
        object size = body.Single(item => Opcode(item) == GcBifOne);
        object[] sizeOperands = Children(size, "Operands").ToArray();
        object[] types = Children(Property(code, "Types"), "Entries").ToArray();
        int? sizeOperandType = (int?)Property(sizeOperands[3], "TypeIndex");
        object heap = body.Single(item => Opcode(item) == TestHeap);
        object failure = body.Single(item => Opcode(item) == CaseEnd);
        if (body.Any(item => Opcode(item) is Allocate or AllocateHeap or Deallocate)
            || Value(sizeOperands[1]) != 1
            || Value(sizeOperands[^1]) != 1
            || sizeOperandType is not int typeIndex
            || (byte)Property(types[typeIndex], "SizeUnit") != BitSyntaxDefaults.BinaryUnit
            || Value(Children(heap, "Operands").Last()) != 2
            || Value(Children(failure, "Operands").First()) != 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] encode = Body("encode");
        object[] decode = Body("decode");
        object[] encodeSize = Children(encode.Single(item => Opcode(item) == GcBifOne), "Operands").ToArray();
        object[] allocations = decode.Where(item => Opcode(item) is Allocate or AllocateHeap).ToArray();
        if (encode.Any(item => Opcode(item) is Allocate or AllocateHeap or Deallocate)
            || (int?)Property(encodeSize[3], "TypeIndex") != sizeOperandType
            || !encode.Where(item => Opcode(item) == CaseEnd)
                .Select(item => Value(Children(item, "Operands").Single())).SequenceEqual([1, 0])
            || allocations.Length != 1
            || !Children(allocations[0], "Operands").Select(Value).SequenceEqual([0, 2])
            || Array.FindIndex(decode, item => Opcode(item) == Return) >= Array.IndexOf(decode, allocations[0])
            || Value(Children(decode.Single(item => Opcode(item) == CaseEnd), "Operands").Single()) != 0)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] Body(string name)
        {
            int index = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo
                && atoms[(int)Value(Children(item, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(index + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

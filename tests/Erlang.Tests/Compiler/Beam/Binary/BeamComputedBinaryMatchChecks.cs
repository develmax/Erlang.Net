using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamComputedBinaryMatchChecks
{
    internal const string TestName = "compiler/beam/keeps-computed-binary-width-in-registers";
    private const string Fixture = "beam_computed_binary_match_probe.erl";
    private const string WrongRoots = "Computed-width binary matching must preserve the original value in X0 and only the numeric input in Y0.";
    private const string WrongTailUnit = "A materialized binary tail must preserve the proven bitstring size unit.";
    private const string Source = """
        -module(beam_computed_binary_match_probe).
        -export([wrap/1]).

        wrap(0) -> <<0>>;
        wrap(Value) when 0 < Value ->
            binary:encode_unsigned(Value);
        wrap(Value) when Value < 0 ->
            Size = bit_size(binary:encode_unsigned(-Value)),
            Width = Size + 8,
            <<Offset:Width>> = <<1,0:Size>>,
            <<Head, Rest/binary>> = binary:encode_unsigned(Offset + Value),
            case Head band 128 of
                128 -> <<(byte_size(Rest)+1):32,Head,Rest/binary>>;
                _ -> <<(byte_size(Rest)+2):32,255,Head,Rest/binary>>
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == StartBinaryMatchKnown);
        if (start < 0 || start + 2 >= instructions.Length
            || Opcode(instructions[start + 1]) != GetBinaryInteger || Opcode(instructions[start + 2]) != BinaryMatch
            || Value(instructions[start], 3) != 2 || Value(instructions[start + 1], 2) != 3
            || Value(instructions[start + 1], 3) != 1 || Value(instructions[start + 1], 6) != 1
            || Value(instructions[start + 2], 1) != 2
            || !instructions.Take(start).Any(instruction =>
                Opcode(instruction) == Allocate && Value(instruction, 0) == 1 && Value(instruction, 1) == 2))
        {
            throw new InvalidOperationException(WrongRoots);
        }

        object[] types = Items(Property(code, "Types"), "Entries").ToArray();
        object[] constructors = instructions.Skip(start + 2)
            .Where(instruction => Opcode(instruction) == CreateBinary).ToArray();
        if (constructors.Length != 2 || constructors.Any(constructor =>
        {
            object tail = Items(constructor, "Operands").SkipLast(1).Last();

            return Property(tail, "TypeIndex") is not int type
                || (byte)Property(types[type], "SizeUnit") != BitSyntaxDefaults.BinaryUnit;
        }))
        {
            throw new InvalidOperationException(WrongTailUnit);
        }
    }

    private static int Value(object instruction, int operand) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(operand), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

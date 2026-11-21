using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLongLiteralBinaryPrefixChecks
{
    internal const string TestName = "compiler/beam/uses-string-matches-for-long-unbound-binary-prefixes";
    private const string PrefixFailure = "The emitted binary prefix length or alignment check differs.";

    private const string Source = """
        -module(beam_long_binary_prefix_probe).
        -export([bytes/1, bits/1, alias/1, boundary/1]).
        bytes(<<"-----BEGIN", _/binary>>) -> starts;
        bytes(_) -> other.
        bits(<<"-----BEGIN", _/bitstring>>) -> starts;
        bits(_) -> other.
        alias(<<"0123456789", _/binary>> = Whole) -> {starts, Whole};
        alias(Value) -> {other, Value}.
        boundary(<<"12345678", _/binary>>) -> starts;
        boundary(_) -> other.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_long_binary_prefix_probe.erl"));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Verify("bytes", true);
        Verify("bits", false);
        Verify("alias", true);
        if (Body("boundary").Any(instruction => Opcode(instruction) == MatchBinaryString))
        {
            throw new InvalidOperationException(PrefixFailure);
        }

        return Task.CompletedTask;

        void Verify(string function, bool aligned)
        {
            object[] body = Body(function);
            int index = Array.FindIndex(body, instruction => Opcode(instruction) == MatchBinaryString);
            if (index < 0 || (int)(BigInteger)Property(Items(body[index], "Operands").ElementAt(2), "Value") != 80
                || (Opcode(body[index + 1]) == BinaryMatch) != aligned)
            {
                throw new InvalidOperationException(PrefixFailure);
            }
        }

        object[] Body(string function)
        {
            int index = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );

            return instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}

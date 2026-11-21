using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamReflexiveExactTestsChecks
{
    internal const string TestName = "compiler/beam/removes-exact-guards-reading-one-register-twice";
    private const string Fixture = "beam_reflexive_exact_probe.erl";
    private const string ReflexiveGuardRemains = "A tuple pattern retained an exact comparison of one register with itself.";
    private const string DifferentOperandGuardMissing = "A tuple pattern lost the exact comparison between different registers.";

    private const string Source = """
        -module(beam_reflexive_exact_probe).
        -export([choice/1, same/2]).
        choice({item, Direct, Indirect, Descriptor, Encoding} = Original) ->
            Tag =
                case {Direct, Indirect} of
                    {Direct, missing} -> {direct, Direct};
                    {missing, Indirect} -> {indirect, Indirect};
                    _ -> {both, Indirect, Direct}
                end,
            case Encoding of
                {octets, Bytes} when is_list(Bytes); is_binary(Bytes) ->
                    {item, Tag, Descriptor, Bytes};
                _ -> Original
            end.

        same(First, Second) ->
            case {First, Second} of
                {First, First} -> equal;
                _ -> different
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] comparisons = Items(code, "Instructions").Where(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode") == ExactEqualTest).ToArray();
        bool hasDifferentRegisters = false;
        foreach (object comparison in comparisons)
        {
            object[] operands = Items(comparison, "Operands").ToArray();
            if (operands is [_, var left, var right]
                && (string)Property(left, "Kind") == "x" && (string)Property(right, "Kind") == "x")
            {
                if ((BigInteger)Property(left, "Value") == (BigInteger)Property(right, "Value"))
                {
                    throw new InvalidOperationException(ReflexiveGuardRemains);
                }

                hasDifferentRegisters = true;
            }
        }

        if (!hasDifferentRegisters)
        {
            throw new InvalidOperationException(DifferentOperandGuardMissing);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

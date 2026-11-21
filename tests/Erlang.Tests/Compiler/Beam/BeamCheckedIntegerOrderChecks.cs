using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedIntegerOrderChecks
{
    internal const string TestName = "compiler/beam/canonicalizes-proven-integer-order-comparisons";
    internal const string Fixture = "beam_checked_integer_order_probe.erl";
    private const string IncorrectBound = "A proven integer comparison did not use the adjacent inclusive bound.";
    private const string ChangedGeneralOrder = "An unproven integer comparison lost strict Erlang term ordering.";

    internal const string Source = """
        -module(beam_checked_integer_order_probe).
        -export([positive/1, negative/1, limit/1, opaque/1, numeric/1]).

        positive(Value) when is_integer(Value), Value > 0 -> yes;
        positive(_) -> no.
        negative(Value) when is_integer(Value), Value < 0 -> yes;
        negative(_) -> no.
        limit(Value) when is_integer(Value), Value < 134217728 -> yes;
        limit(_) -> no.
        opaque(Value) when Value > 0 -> yes;
        opaque(_) -> no.
        numeric(Value) when is_number(Value), Value > 0 -> yes;
        numeric(_) -> no.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] ops = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, ops.Length).Where(index => ops[index] == FunctionInfo).ToArray();
        BigInteger[] bounds = [BigInteger.One, BigInteger.MinusOne, new BigInteger(134217727)];
        for (int function = 0; function < bounds.Length; function++)
        {
            int start = entries[function];
            int end = entries[function + 1];
            int compare = Array.IndexOf(
                ops,
                IsGreaterOrEqual,
                start,
                end - start
            );
            if (compare < 0)
            {
                throw new InvalidOperationException(IncorrectBound);
            }

            object[] operands = ((IEnumerable)Property(instructions[compare], "Operands")).Cast<object>().ToArray();
            int bound = function == 0 ? 2 : 1;
            int register = 3 - bound;
            if ((BigInteger)Property(operands[bound], "Value") != bounds[function]
                || Property(operands[register], "TypeIndex") is not int)
            {
                throw new InvalidOperationException(IncorrectBound);
            }
        }

        for (int function = bounds.Length; function < 5; function++)
        {
            int start = entries[function];
            if (Array.IndexOf(
                ops,
                IsLess,
                start,
                entries[function + 1] - start
            ) < 0)
            {
                throw new InvalidOperationException(ChangedGeneralOrder);
            }
        }

        return Task.CompletedTask;
    }
}

using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

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

        var compiler = new BeamModuleCompiler("beam_checked_interval_fixture");
        _ = Call(compiler, "Intern", "run");
        if (Rewrite(CheckedInterval()) is not { } annotated || !annotated.SequenceEqual(CheckedInterval(annotated: true))
            || Rewrite(annotated) is not null
            || Rewrite(CheckedInterval(interveningWrite: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectBound);
        }

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var emittedTypes = ((IEnumerable)typeof(BeamModuleCompiler).GetField("typeBits", flags)!.GetValue(compiler)!)
            .Cast<(int Bits, long? Lower, long? Upper, int? Unit)>().ToArray();
        if (emittedTypes.Length != 3 || emittedTypes[1].Lower is not null || emittedTypes[1].Upper is not null
            || emittedTypes[2].Lower != 7L || emittedTypes[2].Upper is not null
            || emittedTypes[1].Bits != emittedTypes[2].Bits)
        {
            throw new InvalidOperationException(IncorrectBound);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> fixture) =>
            (byte[]?)Call(
                compiler,
                "AnnotateCheckedIntegerOrderInputs",
                fixture,
                5
            );

    }

    private static byte[] CheckedInterval(bool annotated = false, bool interveningWrite = false)
    {
        byte[] first = annotated ? [ExtendedTypedRegisterOperand, X(0), U(1)] : [X(0)];
        byte[] second = annotated ? [ExtendedTypedRegisterOperand, X(1), U(1)] : [X(1)];
        byte[] lower = annotated ? [ExtendedTypedRegisterOperand, X(0), U(2)] : [X(0)];
        byte[] overwrite = interveningWrite ? [Move, A(2), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            IsInteger, F(4), X(0), IsInteger, F(3), X(1), .. overwrite,
            IsGreaterOrEqual, F(3), .. first, I(7),
            IsGreaterOrEqual, F(3), .. second, .. lower,
            Return,
            Label, U(3), IsGreaterOrEqual, F(4), X(0), I(7), Return,
            Label, U(4), Return, EndCode
        ];
    }
}

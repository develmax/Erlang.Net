using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCallLiteralComparisonOrderChecks
{
    internal const string TestName = "compiler/beam/orders-an-exact-call-comparison-before-its-literal";
    private const string Module = "beam_exact_call_comparison";
    private const string WrongComparison = "Expected the exact comparison to retain the call result as its first operand.";

    private const string Source = """
        -module(beam_exact_call_comparison).
        -export([different/1, same/1, retained/2]).
        different(Value) -> absent =/= beam_exact_call_peer:read(Value).
        same(Value) -> absent =:= beam_exact_call_peer:read(Value).
        retained(Value, Expected) -> Expected =/= beam_exact_call_peer:read(Value).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("different", "x", "atom");
        Check("same", "x", "atom");
        Check("retained", "y", "x");

        return Task.CompletedTask;

        void Check(string name, string leftKind, string rightKind)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongComparison);
            }

            object[] operations = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is ExternalCall or BifTwo).ToArray();
            if (operations is not [var call, var comparison] || Opcode(call) != ExternalCall || Opcode(comparison) != BifTwo
                || Operands(comparison) is not [_, _, var left, var right, var destination]
                || (string)Property(left, "Kind") != leftKind || (string)Property(right, "Kind") != rightKind
                || (string)Property(destination, "Kind") != "x" || Value(destination) != 0)
            {
                throw new InvalidOperationException(WrongComparison);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

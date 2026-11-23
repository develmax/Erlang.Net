using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralColumnPairsChecks
{
    internal const string TestName = "compiler/beam/groups-two-literal-columns";
    private const string Fixture = "beam_literal_column_pairs_probe.erl";
    private const string UnexpectedPredicate = "Expected grouped singleton predicates to return the comparison directly.";
    private const string UnexpectedMatrix = "Expected integer-keyed literal pairs to share their suffix dispatch.";

    internal const string Source = """
        -module(beam_literal_column_pairs_probe).
        -export([lookup/2, overlaps/2, guarded/2, subset/2]).

        lookup(9, high) -> {seen,high};
        lookup(9, low) -> {seen,low};
        lookup(9, _) -> missing;
        lookup(15, high) -> {seen,high};
        lookup(15, low) -> {seen,low};
        lookup(15, _) -> missing;
        lookup(30, _) -> open;
        lookup(_, _) -> unknown.

        overlaps(9, _) -> first;
        overlaps(9, high) -> shadowed;
        overlaps(15, _) -> second;
        overlaps(_, _) -> unknown.

        guarded(9, Key) when is_float(Key) -> float_value;
        guarded(9, _) -> first;
        guarded(15, _) -> second;
        guarded(_, _) -> unknown.

        subset(disjoint, 0) -> true;
        subset(disjoint, 2) -> true;
        subset(single, 1) -> true;
        subset(_, _) -> false.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] table = Function("lookup").Where(instruction => Opcode(instruction) == SelectValue).ToArray();
        if (table is not [var outer, var inner] || Value(Items(outer, "Operands").First()) != 0
            || Value(Items(inner, "Operands").First()) != 1)
        {
            throw new InvalidOperationException(UnexpectedMatrix);
        }

        var targets = Items(outer, "Operands").Skip(3).Chunk(2).ToDictionary(pair => Value(pair[0]), pair => Value(pair[1]));
        if (targets[9] != targets[15] || targets[9] == targets[30])
        {
            throw new InvalidOperationException(UnexpectedMatrix);
        }

        if (!Function("subset").Any(instruction => Opcode(instruction) == BifTwo))
        {
            throw new InvalidOperationException(UnexpectedPredicate);
        }

        IEnumerable<object> Function(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(start + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

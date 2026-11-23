using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralColumnDispatchChecks
{
    internal const string TestName = "compiler/beam/groups-literal-columns-and-shares-fallbacks";
    private const string Fixture = "beam_literal_column_dispatch_probe.erl";
    private const string UnexpectedBoolean = "Expected grouped Boolean arities to return comparisons directly.";
    private const string UnexpectedMatrix = "Expected a shared literal-column decision graph.";

    internal const string Source = """
        -module(beam_literal_column_dispatch_probe).
        -export([route/3, mixed/3, guarded/3, repeated/3, classify/3]).

        route(alpha, item, 1) -> {accepted,first};
        route(alpha, item, 2) -> {accepted,second};
        route(alpha, item, _) -> {rejected,bad_index};
        route(alpha, _, _) -> {rejected,bad_item};
        route(beta, item, 1) -> {accepted,first};
        route(beta, item, 2) -> {accepted,second};
        route(beta, item, _) -> {rejected,bad_index};
        route(beta, _, _) -> {rejected,bad_item};
        route(gamma, _, _) -> open;
        route(_, _, _) -> absent.

        mixed(alpha, _, 1) -> first;
        mixed(alpha, item, _) -> second;
        mixed(beta, _, _) -> third;
        mixed(_, _, _) -> absent.

        guarded(alpha, item, Number) when is_integer(Number), Number < 0 -> guarded;
        guarded(alpha, item, _) -> plain;
        guarded(beta, _, _) -> open;
        guarded(_, _, _) -> absent.

        repeated(alpha, Value, Value) -> equal;
        repeated(beta, _, _) -> open;
        repeated(_, _, _) -> different.

        classify(alpha, head, 1) -> true;
        classify(alpha, head, 2) -> true;
        classify(alpha, tail, 1) -> true;
        classify(beta, head, 0) -> true;
        classify(beta, tail, 1) -> true;
        classify(_, _, _) -> false.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == "route"
        );
        object[] table = instructions.Skip(start + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) == SelectValue).ToArray();
        if (table is not [var modules, var indexes] || Value(Items(modules, "Operands").First()) != 0
            || Value(Items(indexes, "Operands").First()) != 2)
        {
            throw new InvalidOperationException(UnexpectedMatrix);
        }

        var targets = Items(modules, "Operands").Skip(3).Chunk(2)
            .ToDictionary(pair => atoms[Value(pair[0])], pair => Value(pair[1]), StringComparer.Ordinal);
        if (targets["alpha"] != targets["beta"] || targets["alpha"] == targets["gamma"])
        {
            throw new InvalidOperationException(UnexpectedMatrix);
        }

        int booleanStart = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == "classify"
        );
        if (!instructions.Skip(booleanStart + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == BifTwo))
        {
            throw new InvalidOperationException(UnexpectedBoolean);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

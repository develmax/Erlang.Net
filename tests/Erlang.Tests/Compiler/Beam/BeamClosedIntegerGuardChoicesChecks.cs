using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedIntegerGuardChoicesChecks
{
    internal const string TestName = "compiler/beam/utf-pattern-bounds-feed-private-integer-guard-table";
    private const string ShapeError = "The private integer guard table did not follow the proved decode bounds.";
    private const string Source = """
        -module(beam_closed_integer_guard_choices).
        -export([decode/1]).

        decode(<<Code/utf8>>) -> [classify(Code, 0)].

        classify(Code, Position) when Code == $.; Code == $?; Code == $* ->
            {list_to_atom([Code]), Position};
        classify(Code, Position) when $0 =< Code, Code =< $9 ->
            {digit, Position, Code};
        classify(Code, Position) ->
            {other, Position, Code}.
        """;

    internal static void Run()
    {
        string exported = Source.Replace("decode/1]", "decode/1,classify/2]", StringComparison.Ordinal);
        string floatCaller = Source.Replace("classify(Code, 0)].", "classify(Code, 0)]; decode(loose) -> [classify(42.0, 0)].", StringComparison.Ordinal);
        if (!HasIntegerGuardTable(Source) || HasIntegerGuardTable(exported) || HasIntegerGuardTable(floatCaller))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasIntegerGuardTable(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_closed_integer_guard_choices.erl"));
        var atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string?>().ToArray();
        var operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo || atoms[Value(Operands(operation)[1])] != "classify")
            .Skip(1).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();

        return operations.Any(operation => Opcode(operation) == SelectValue)
            && operations.Any(operation => Opcode(operation) == AllocateHeap && Value(Operands(operation)[0]) == 1)
            && operations.Count(operation => Opcode(operation) == IsGreaterOrEqual) == 2;
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}

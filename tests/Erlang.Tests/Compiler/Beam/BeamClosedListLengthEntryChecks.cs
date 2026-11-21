using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedListLengthEntryChecks
{
    internal const string TestName = "compiler/beam/shares-prefix-for-disjoint-proper-list-lengths";
    private const string ShapeError = "The proper-list clauses did not share their checked entry prefix.";
    private const string Module = "beam_closed_list_length_entry";

    private const string Source = """
        -module(beam_closed_list_length_entry).
        -export([pick/1]).
        pick([A,B]) -> service:short(A,B);
        pick([A,B,C]) -> service:long(A,B,C).
        """;

    internal static void Run()
    {
        string longerFirst = Source.Replace(
            "pick([A,B]) -> service:short(A,B);\npick([A,B,C]) -> service:long(A,B,C).",
            "pick([A,B,C]) -> service:long(A,B,C);\npick([A,B]) -> service:short(A,B).",
            StringComparison.Ordinal
        );
        string checkedHead = Source.Replace("pick([A,B]) -> service:short(A,B)", "pick([yes,B]) -> service:short(yes,B)", StringComparison.Ordinal);
        if (longerFirst == Source || checkedHead == Source || !SharesEntryPrefix(Source)
            || !SharesEntryPrefix(longerFirst) || SharesEntryPrefix(checkedHead))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool SharesEntryPrefix(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        var cells = instructions.Where(instruction => Opcode(instruction) == GetList).ToArray();
        if (cells.Length != 3 || instructions.Count(instruction => Opcode(instruction) == IsNil) != 2)
        {
            return false;
        }

        return Children(cells[0], "Operands").Select(Value).SequenceEqual([0, 1, 2])
            && Children(cells[1], "Operands").Select(Value).SequenceEqual([2, 3, 2])
            && Children(cells[2], "Operands").Select(Value).SequenceEqual([2, 4, 2]);
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}

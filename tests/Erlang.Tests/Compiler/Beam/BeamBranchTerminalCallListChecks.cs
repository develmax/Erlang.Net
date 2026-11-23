using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBranchTerminalCallListChecks
{
    internal const string TestName = "compiler/beam/empty-list-fast-arm-precedes-terminal-call-list-frame";
    private const string ShapeError = "The two-clause call list did not retain the branch-local frame.";
    private const string Source = """
        -module(beam_branch_terminal_call_list).
        -export([wrap/3]).

        wrap(Tag, Attrs, []) -> empty(Tag, Attrs);
        wrap(Tag, Attrs, Data) -> [first(Tag, Attrs), Data, last(Tag)].

        empty(Tag, Attrs) -> {empty,Tag,Attrs}.
        first(Tag, Attrs) -> {first,Tag,Attrs}.
        last(Tag) -> {last,Tag}.
        """;

    internal static void Run()
    {
        var instructions = Instructions(Source);
        var guarded = Instructions(Source.Replace("wrap(Tag, Attrs, []) ->", "wrap(Tag, Attrs, []) when is_atom(Tag) ->", StringComparison.Ordinal));
        if (!HasNilFastArm(instructions) || HasNilFastArm(guarded))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasNilFastArm(object[] instructions)
    {
        int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo);
        if (start < 0)
        {
            return false;
        }

        var body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) != Label && Opcode(instruction) != Line).ToArray();
        if (body.Length < 3 || Opcode(body[0]) != IsNil || Opcode(body[1]) != TailLocalCall
            || Opcode(body[2]) != Allocate || Value(Operands(body[2])[0]) != 2)
        {
            return false;
        }

        return body.Any(instruction => Opcode(instruction) == Swap
            && Operands(instruction).Any(operand => Kind(operand) == BeamBinaryHostNames.Y));
    }

    private static object[] Instructions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_branch_terminal_call_list.erl"));

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
    }

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}

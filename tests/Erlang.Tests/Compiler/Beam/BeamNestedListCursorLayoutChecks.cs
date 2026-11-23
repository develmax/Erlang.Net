using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListCursorLayoutChecks
{
    internal const string TestName = "compiler/beam/uses-direct-cursor-registers-in-nested-list-helpers";
    private const string Fixture = "beam_nested_list_cursor_layout_probe.erl";
    private const string UnexpectedLayout = "Direct nested continuation cursors did not use the pinned register layout.";

    internal const string Source = """
        -module(beam_nested_list_cursor_layout_probe).
        -export([read_all/1, pairs/1]).

        read_all(Rows) ->
            [Value || Row <- Rows,
                      Value <- read(Row)].

        pairs(Mode) ->
            First = [a, b],
            Second = [one, two],
            case Mode of
                enabled -> [[X, Y] || X <- First, Y <- Second];
                _ -> disabled
            end.

        read({row, Key, Items}) ->
            self() ! {read, Key},
            Items;
        read({fail, Reason}) ->
            self() ! {failed, Reason},
            erlang:error({source_failed, Reason}).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int cursorOuter = Find("-read_all/1-lc$^0/1-0-", 1);
        int cursorInner = Find("-read_all/1-lc$^1/1-1-", 2);
        int closedOuter = Find("-pairs/1-lc$^0/1-0-", 1);
        int closedInner = Find("-pairs/1-lc$^1/1-1-", 3);
        if (cursorOuter < 0 || cursorInner <= cursorOuter || closedOuter < 0 || closedInner <= closedOuter
            || !HasPrefix(cursorOuter, [IsNonemptyList, Allocate, GetList])
            || !HasPrefix(cursorInner, [IsNonemptyList, Allocate, GetList, LocalCall, TestHeap, PutList])
            || !HasPrefix(closedOuter, [IsNonemptyList, GetList, Move, TailLocalCall])
            || !HasPrefix(closedInner, [IsNonemptyList, Allocate, Move, GetList, LocalCall, TestHeap, PutList, PutList, PutList])
            || Body(closedOuter).Any(instruction => Opcode(instruction) == IsNil)
            || Body(closedInner).Any(instruction => Opcode(instruction) == IsNil))
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        int Find(string name, int width) => Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == name && Value(arity) == width
        );

        IEnumerable<object> Body(int info) => instructions.Skip(info + 2)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo);

        bool HasPrefix(int info, byte[] expected) => Body(info).Take(expected.Length)
            .Select(Opcode).SequenceEqual(expected);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

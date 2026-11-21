using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedEntryListFrameChecks
{
    internal const string TestName = "compiler/beam/projects-checked-entry-list-directly-into-call-frame";
    private const string Fixture = "beam_projected_entry_list_frame_probe.erl";
    private const string StagedProjection = "The checked list still keeps projection scratch registers live at allocation.";

    internal const string Source = """
        -module(beam_projected_entry_list_frame_probe).
        -export([tagged/2, retained/2]).

        tagged(List, Target) ->
            [begin
                 notify(Item, Target),
                 {ok, Value} = read(Item),
                 {Item, Value, done}
             end || Item <- List].

        retained([Head | Tail], Target) ->
            notify(Head, Target),
            Tail;
        retained([], _Target) ->
            [].

        notify(Item, Target) -> Target ! {seen, Item}.
        read(Item) -> {ok, {value, Item}}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int function = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == "-tagged/2-lc$^0/1-0-"
        );
        object[] body = instructions.Skip(function + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int check = Array.FindIndex(body, instruction => Opcode(instruction) == IsNonemptyList);
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int projection = Array.FindIndex(body, instruction => Opcode(instruction) == GetList);
        if (function < 0 || check < 0 || frame <= check || projection <= frame || Value(Operands(body[frame])[^1]) != 2
            || Operands(body[projection]) is not [var source, var head, var tail]
            || !Register(source, "x") || Value(source) != 0 || !Register(head, "y") || !Register(tail, "y")
            || Value(head) == Value(tail))
        {
            throw new InvalidOperationException(StagedProjection);
        }
    }

    private static bool Register(object operand, string kind) => (string)Property(operand, "Kind") == kind;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

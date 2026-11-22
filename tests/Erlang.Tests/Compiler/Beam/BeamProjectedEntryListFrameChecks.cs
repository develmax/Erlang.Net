using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedEntryListFrameChecks
{
    internal const string TestName = "compiler/beam/projects-checked-entry-list-directly-into-call-frame";
    private const string Fixture = "beam_projected_entry_list_frame_probe.erl";
    private const string StagedProjection = "The checked list still keeps projection scratch registers live at allocation.";
    private const string OutgoingProjection = "The entry projection must rewrite only verified outgoing scratch copies.";
    private const string CollectingPrelude = "A checked list projection must keep the collecting prelude and its source location.";
    private const string MixedProjection = "A direct list projection must preserve the preceding and outgoing argument values.";

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

        CheckOutgoingProjectionCopies();
        CheckMixedOutgoingProjectionCopies();
        CheckCollectingPrelude();
    }

    private static void CheckOutgoingProjectionCopies()
    {
        var compiler = new BeamModuleCompiler("outgoing_projection_fixture");
        byte[] entry = [.. Start(
            1,
            2,
            1,
            1
        ), IsNonemptyList, F(3), X(0)];
        byte[] original =
        [
            GetList, X(0), X(1), X(2), Allocate, U(2), U(3),
            Move, X(2), Y(0), Move, X(1), Y(1)
        ];
        byte[] rewrite = [Allocate, U(2), U(1), GetList, X(0), Y(1), Y(0), Move, Y(1), X(0)];
        byte[] returning = [Deallocate, U(2), Return, Label, U(3), Return, EndCode];
        byte[] transfer = [LocalCall, U(1), F(2)];
        byte[] before = [.. entry, .. original, Move, X(1), X(0), .. transfer, .. returning];
        byte[] expected = [.. entry, .. rewrite, .. transfer, .. returning];
        if (Apply(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(OutgoingProjection);
        }

        byte[] changedTemporary =
        [
            .. entry, .. original, Move, X(1), X(0), Move, X(0), X(1), .. transfer, .. returning
        ];
        byte[] liveTemporary = [.. entry, .. original, ExternalCall, U(2), U(0), .. returning];
        if (Apply(changedTemporary) is not null || Apply(liveTemporary) is not null)
        {
            throw new InvalidOperationException(OutgoingProjection);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ProjectEntryListDirectlyIntoFrame",
            (ReadOnlyMemory<byte>)bytes,
            4
        );
    }

    private static void CheckMixedOutgoingProjectionCopies()
    {
        var compiler = new BeamModuleCompiler("mixed_outgoing_projection_fixture");
        byte[] entry =
        [
            .. Start(
                1,
                2,
                1,
                2
            ), IsNonemptyList, F(3), X(0)
        ];
        byte[] original = [GetList, X(0), X(2), X(3), Allocate, U(1), U(4)];
        byte[] transfer = [ExternalCall, U(2), U(0), Deallocate, U(1), Return, Label, U(3), Return, EndCode];
        byte[] headInX = [.. entry, .. original, Move, X(3), Y(0), Move, X(2), X(0), .. transfer];
        byte[] headExpected = [.. entry, Allocate, U(1), U(2), GetList, X(0), X(0), Y(0), .. transfer];
        byte[] tailInX =
        [
            .. entry, .. original, Move, X(2), Y(0), Move, X(2), X(1), Move, X(3), X(0), .. transfer
        ];
        byte[] tailExpected =
        [
            .. entry, Allocate, U(1), U(2), GetList, X(0), Y(0), X(0), Move, Y(0), X(1), .. transfer
        ];
        if (Apply(headInX) is not { } headActual || !headActual.AsSpan().SequenceEqual(headExpected)
            || Apply(tailInX) is not { } tailActual || !tailActual.AsSpan().SequenceEqual(tailExpected))
        {
            throw new InvalidOperationException(MixedProjection);
        }

        byte[] readsOldArgument =
        [
            .. entry, .. original, Move, X(3), Y(0), Move, X(0), X(1), Move, X(2), X(0), .. transfer
        ];
        byte[] rebindsArgument =
        [
            .. entry, .. original, Move, X(3), Y(0), Move, X(2), X(0), Move, X(1), X(0), .. transfer
        ];
        byte[] readsScratchTwice =
        [
            .. entry, .. original, Move, X(3), Y(0), Move, X(2), X(0), Move, X(2), X(1), .. transfer
        ];
        if (Apply(readsOldArgument) is not null || Apply(rebindsArgument) is not null
            || Apply(readsScratchTwice) is not null)
        {
            throw new InvalidOperationException(MixedProjection);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ProjectEntryListDirectlyIntoFrame",
            (ReadOnlyMemory<byte>)bytes,
            4
        );
    }

    private static void CheckCollectingPrelude()
    {
        var compiler = new BeamModuleCompiler("collecting_projection_fixture");
        byte[] entry =
        [
            .. Start(
                1,
                2,
                1,
                3
            ), IsNonemptyList, F(3), X(0)
        ];
        byte[] allocation = [Allocate, U(4), U(5), Move, X(2), Y(2), Move, X(1), Y(3)];
        byte[] scratchSaves = [Move, X(4), Y(0), Move, X(3), Y(1)];
        byte[] calculation = [GcBifTwo, F(0), U(0), U(0), Y(2), 0x11, X(0)];
        byte[] continuation = [LocalCall, U(1), F(2), Deallocate, U(4), Return, Label, U(3), Return, EndCode];
        byte[] original =
        [
            .. entry, GetList, X(0), X(3), X(4), Line, U(1),
            .. allocation, .. scratchSaves, .. calculation, .. continuation
        ];
        byte[] expected =
        [
            .. entry, Allocate, U(4), U(3), Move, X(2), Y(2), Move, X(1), Y(3),
            GetList, X(0), Y(1), Y(0), Line, U(1), .. calculation, .. continuation
        ];
        if (Apply(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(CollectingPrelude);
        }

        byte[] rootsScratch = (byte[])original.Clone();
        rootsScratch[entry.Length + 6 + allocation.Length + scratchSaves.Length + 2] = U(4);
        byte[] usesScratch = (byte[])original.Clone();
        usesScratch[entry.Length + 6 + allocation.Length + scratchSaves.Length + 4] = X(3);
        if (Apply(rootsScratch) is not null || Apply(usesScratch) is not null)
        {
            throw new InvalidOperationException(CollectingPrelude);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ProjectEntryListDirectlyIntoFrame",
            (ReadOnlyMemory<byte>)bytes,
            4
        );
    }

    private static bool Register(object operand, string kind) => (string)Property(operand, "Kind") == kind;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

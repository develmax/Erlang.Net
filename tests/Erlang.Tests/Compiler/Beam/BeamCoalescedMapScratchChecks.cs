using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCoalescedMapScratchChecks
{
    internal const string TestName = "compiler/beam/coalesces-checked-map-scratch-projections";
    private const string Failure = "Adjacent map scratch copies must preserve failure-visible values.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_map_scratch_probe");
        byte[] first = [GetMapElements, F(3), X(1), ExtendedListOperand, U(2), A(2), X(0)];
        byte[] second = [GetMapElements, F(3), X(1), ExtendedListOperand, U(2), A(3), X(0)];
        byte[] corridor = [.. first, Move, X(0), X(2), .. second];
        byte[] grouped = [GetMapElements, F(3), X(1), ExtendedListOperand, U(4), A(3), X(0), A(2), X(2)];

        if (Rewrite(Fixture(corridor)) is not { } rewritten
            || !rewritten.SequenceEqual(Fixture(grouped))
            || Rewrite(Fixture(corridor, 2)) is not null
            || Rewrite(Fixture([.. first, Move, X(0), X(1), .. second])) is not null
            || Rewrite(Fixture([.. first, Move, X(0), X(2), .. first])) is not null
            || RemoveRedundant(RepeatedMapCheck(second)) is not { } optimized
            || !optimized.SequenceEqual(RepeatedMapCheck(second, repeatedCheck: false))
            || RemoveRedundant(RepeatedMapCheck(second, liveRoots: 1)) is not null
            || RemoveRedundant(RepeatedMapCheck(second, failureRejoins: true)) is not null
            || RemoveRedundant(RepeatedStackMapCheck()) is not { } stack
            || !stack.SequenceEqual(RepeatedStackMapCheck(repeatedCheck: false))
            || RemoveRedundant(RepeatedStackMapCheck(overwrite: true)) is not null
            || RemoveRedundant(RepeatedStackMapCheck(failureRejoins: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        CheckTerminalBranches(compiler);
        CheckOutlineMapFailures(compiler);

        return Task.CompletedTask;

        byte[]? RemoveRedundant(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RemoveDominatedMapProjectionTests",
            code,
            6
        );

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "CoalesceFinalMapScratchProjections",
            code,
            4
        );
    }

    private static void CheckTerminalBranches(BeamModuleCompiler compiler)
    {
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "extra");
        _ = Call(compiler, "Intern", "missing");
        using var literal = new MemoryStream();
        _ = Call(
            compiler,
            "WriteLiteralPoolOperand",
            literal,
            new MapTerm([])
        );

        byte[] first = [PutMapAssoc, F(0), ExtendedLiteralOperand, U(0), X(0), U(3), ExtendedListOperand, U(2), A(2), X(2)];
        byte[] returned = [Move, X(0), X(2)];
        byte[] read = [GetMapElements, F(4), X(1), ExtendedListOperand, U(2), A(3), X(0)];
        byte[] update = [PutMapAssoc, F(0), X(2), X(0), U(3), ExtendedListOperand, U(2), A(3), X(1)];
        byte[] old = [.. first, .. returned, .. read, Move, X(0), X(1), ExactEqualTest, F(3), X(1), A(4),
            Move, X(2), X(0), Return, Label, U(3), .. update, Return];

        byte[] rewritten =
        [
            PutMapAssoc, F(0), ExtendedLiteralOperand, U(0), X(3), U(3), ExtendedListOperand, U(2), A(2), X(2),
            GetMapElements, F(4), X(1), ExtendedListOperand, U(2), A(3), X(4),
            ExactEqualTest, F(3), X(4), A(4), Move, X(3), X(0), Return, Label, U(3),
            PutMapAssoc, F(0), ExtendedLiteralOperand, U(0), X(0), U(5), ExtendedListOperand, U(4),
            A(2), X(2), A(3), X(4), Return
        ];
        if (Fuse(Body(old)) is not { } actual || !actual.SequenceEqual(Body(rewritten))
            || Fuse(Body(old, preserveStored: true)) is not null || Fuse(Body(rewritten)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[]? Fuse(ReadOnlyMemory<byte> bytes) => (byte[]?)Call(
            compiler,
            "FuseTerminalLiteralMapBranches",
            bytes,
            5
        );

        static byte[] Body(byte[] code, bool preserveStored = false) =>
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            .. code, Label, U(4), Move, preserveStored ? X(2) : X(1), X(0), CaseEnd, X(0), EndCode
        ];
    }

    private static void CheckOutlineMapFailures(BeamModuleCompiler compiler)
    {
        byte[] before =
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            GetMapElements, F(3), X(1), ExtendedListOperand, U(2), A(2), X(0),
            Jump, F(4), Label, U(3), Move, X(1), X(0), Line, U(1), CaseEnd, X(0),
            Label, U(4), GetMapElements, F(5), X(1), ExtendedListOperand, U(2), A(3), X(0),
            ExactEqualTest, F(6), X(0), A(4), Return,
            Label, U(5), Move, X(1), X(0), Line, U(2), CaseEnd, X(0),
            Label, U(6), Move, X(1), X(0), Return, EndCode
        ];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            GetMapElements, F(6), X(1), ExtendedListOperand, U(2), A(2), X(0),
            Label, U(3), GetMapElements, F(5), X(1), ExtendedListOperand, U(2), A(3), X(0),
            ExactEqualTest, F(4), X(0), A(4), Return,
            Label, U(4), Move, X(1), X(0), Return,
            Label, U(5), Move, X(1), X(0), Line, U(2), CaseEnd, X(0),
            Label, U(6), Move, X(1), X(0), Line, U(1), CaseEnd, X(0), EndCode
        ];
        byte[] differentSource = [.. before];
        int secondLookup = Array.LastIndexOf(differentSource, GetMapElements);
        differentSource[secondLookup + 2] = X(0);
        if (Outline(differentSource) is not null || Outline(before) is not { } outlined
            || !outlined.SequenceEqual(expected) || Outline(outlined) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[]? Outline(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OutlineClosedMapCaseFailures",
            code,
            7
        );
    }

    private static byte[] RepeatedMapCheck(
        byte[] projection,
        bool repeatedCheck = true,
        int liveRoots = 2,
        bool failureRejoins = false
    ) =>
    [
        .. Start(
            1,
            2,
            1,
            2
        ),
        IsMap, F(4), X(1), Jump, F(5),
        Label, U(4),
        .. (failureRejoins ? new byte[] { Jump, F(5) } : [CaseEnd, X(0)]),
        Label, U(5), TestHeap, U(3), U(liveRoots),
        .. (repeatedCheck ? new byte[] { IsMap, F(3), X(1) } : []),
        .. projection,
        Return,
        Label, U(3), CaseEnd, X(0), EndCode
    ];

    private static byte[] RepeatedStackMapCheck(
        bool repeatedCheck = true,
        bool overwrite = false,
        bool failureRejoins = false
    ) =>
    [
        .. Start(
            1,
            2,
            1,
            2
        ),
        Allocate, U(1), U(2), Move, X(1), Y(0),
        IsMap, F(4), Y(0), Jump, F(5),
        Label, U(4),
        .. (failureRejoins ? new byte[] { Jump, F(5) } : [CaseEnd, X(0)]),
        Label, U(5),
        .. (overwrite ? new byte[] { Move, X(0), Y(0) } : []),
        .. (repeatedCheck ? new byte[] { IsMap, F(3), Y(0) } : []),
        GetMapElements, F(3), Y(0), ExtendedListOperand, U(2), A(2), X(0),
        Deallocate, U(1), Return,
        Label, U(3), CaseEnd, X(0), EndCode
    ];

    private static byte[] Fixture(byte[] projection, int failureSource = 1) =>
    [
        .. Start(
            1,
            2,
            1,
            2
        ),
        IsMap, F(3), X(1),
        .. projection,
        Move, X(2), X(0), Return,
        Label, U(3), Move, X(failureSource), X(0), CaseEnd, X(0),
        EndCode
    ];
}

using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedListClosureChecks
{
    internal const string TestName = "compiler/beam/shares-uncaptured-closure-heap-with-retained-list";
    private const string Module = "beam_retained_list_closure_fixture";
    private const string WrongLayout = "Closure construction changed a live saved list, captured variable, or call boundary.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        byte[] expected = Fixture(scheduled: true);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(compiler, actual) is not null
            || Rewrite(compiler, Fixture(laterListRead: true)) is not null
            || Rewrite(compiler, Fixture(reusedClosure: true)) is not null
            || Rewrite(compiler, Fixture(capture: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool scheduled = false,
        bool laterListRead = false,
        bool reusedClosure = false,
        bool capture = false
    )
    {
        byte[] constructor = scheduled
            ? [.. ClosureHeap(ConsHeapWords, 1), PutList, A(1), X(0), Y(0)]
            : [TestHeap, U(ConsHeapWords), U(1), PutList, A(1), X(0), X(0), Move, X(0), Y(0), .. ClosureHeap(0, 0)];
        byte[] captures = capture ? [U(1), Y(0)] : [U(0)];
        byte[] reload = scheduled ? [] : [Move, Y(1), X(0)];
        byte[] clear = scheduled ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] laterRead = laterListRead ? [Move, Y(0), X(0)] : reusedClosure ? [Move, Y(1), X(0)] : [];
        byte width = U(scheduled ? 1 : 2);
        byte[] initial = scheduled ? [U(1), Y(0)] : [U(2), Y(0), Y(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, width, U(0), InitYRegisters, ExtendedListOperand, .. initial,
            Line, U(1), ExternalCall, U(0), U(0),
            .. constructor, MakeFun, U(0), scheduled ? X(0) : Y(1), ExtendedListOperand, .. captures,
            Move, Y(0), X(1), .. reload, Line, U(2), ExternalCall, U(2), U(1),
            .. ClosureHeap(0, 0), MakeFun, U(1), X(0), ExtendedListOperand, U(0),
            Move, Y(0), X(1), .. clear, Line, U(3), ExternalCall, U(2), U(1),
            .. laterRead, Deallocate, width, Return, EndCode
        ];
    }

    private static byte[] ClosureHeap(int words, int live) =>
    [
        TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds), U(AllocationWords),
        U(words), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(live)
    ];

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "ScheduleRetainedListClosures",
            code,
            3
        );
}

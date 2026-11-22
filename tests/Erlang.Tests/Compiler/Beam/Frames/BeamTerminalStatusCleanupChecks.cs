using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalStatusCleanupChecks
{
    internal const string TestName = "compiler/beam/trims-a-terminal-cleanup-to-its-returned-status";
    private const string WrongLayout = "The terminal cleanup lost the resource argument, error result, or original failure edge.";

    internal static Task Run()
    {
        byte[] constructed = Image(useTrim: false);
        byte[] aliased = Image(
            useTrim: false,
            aliasReturn: true,
            resourceAtTop: true
        );
        if (Rewrite(constructed) is not { } reduced
            || !reduced.SequenceEqual(Image(useTrim: true))
            || Rewrite(aliased) is not { } moved
            || !moved.SequenceEqual(Image(
                useTrim: true,
                aliasReturn: true,
                resourceAtTop: true
            ))
            || Rewrite(Image(useTrim: false, sharedFailure: true)) is not null
            || Rewrite(Image(useTrim: false, cleanupUsesDifferentResource: true)) is not null
            || Rewrite(constructed, pure: true) is not null
            || Rewrite(reduced) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool useTrim,
        bool aliasReturn = false,
        bool resourceAtTop = false,
        bool sharedFailure = false,
        bool cleanupUsesDifferentResource = false
    )
    {
        const int width = 3;
        int resource = resourceAtTop ? 2 : 1;
        int kept = useTrim ? 1 : width;
        byte[] sourceReturn = aliasReturn
            ? [Move, Y(1), X(0)]
            : [TestHeap, U(3), U(0), PutTuple, X(0), ExtendedListOperand, U(2), A(2), Y(resource)];
        byte[] anotherEdge = sharedFailure ? [IsTuple, F(3), X(0)] : [];
        int load = cleanupUsesDifferentResource ? resource - 1 : resource;
        byte[] save = useTrim && !resourceAtTop ? [Move, X(0), Y(2)] : [Move, X(0), Y(0)];
        byte[] rebound = useTrim && resourceAtTop ? [Move, Y(0), Y(2)] : [];
        byte[] trim = useTrim ? [TrimFrame, U(2), U(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(width), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Move, X(0), Y(2), Move, X(0), Y(resource), .. anotherEdge,
            Move, Y(resource), X(0), Line, U(1), ExternalCall, U(1), U(0),
            ExactEqualTest, F(3), X(0), A(2),
            .. sourceReturn, Deallocate, U(width), Return,
            Label, U(3), .. save, Move, Y(load), X(0), .. rebound, .. trim,
            Line, U(2), ExternalCall, U(1), U(1), Move, Y(0), X(0),
            Deallocate, U(kept), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code, bool pure = false)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );
        var pureImports = pure ? new HashSet<int> { 1 } : new HashSet<int>();

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTerminalStatusCleanup",
            image,
            body,
            pureImports
        );
    }
}

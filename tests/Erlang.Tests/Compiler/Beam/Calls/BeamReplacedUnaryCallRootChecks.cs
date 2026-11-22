using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamReplacedUnaryCallRootChecks
{
    internal const string TestName = "compiler/beam/clears-an-outgoing-unary-root-before-replacing-its-slot";
    private const string WrongLayout = "A retired Y root may be cleared after copying its value into the call argument.";

    internal static Task Run()
    {
        if (Rewrite(Layout(cleared: false)) is not { } result
            || !result.SequenceEqual(Layout(cleared: true))
            || Rewrite(Layout(cleared: false, preservesSlot: true)) is not null
            || Rewrite(Layout(cleared: false, trimsLater: true)) is not { } trimmed
            || !trimmed.SequenceEqual(Layout(cleared: true, trimsLater: true))
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Layout(bool cleared, bool preservesSlot = false, bool trimsLater = false)
    {
        byte[] retirement = cleared ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];

        byte[] finish = trimsLater
            ? [TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return]
            : [Move, Y(preservesSlot ? 0 : 1), X(0), Deallocate, U(2), Return];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, X(0), Y(0), TestHeap, U(2), U(1), PutList, X(0), A(0), Y(1),
            Move, Y(0), X(0), .. retirement, ExternalCall, U(1), U(0),
            Move, X(0), Y(preservesSlot ? 1 : 0),
            .. finish,
            EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            1,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteDeadRecordCallSlots",
            image,
            body,
            true
        );
    }
}

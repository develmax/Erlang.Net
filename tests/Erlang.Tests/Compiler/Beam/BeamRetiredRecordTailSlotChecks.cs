using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredRecordTailSlotChecks
{
    internal const string TestName = "compiler/beam/reuses-a-retired-record-slot-before-partial-trim";
    private const string WrongLayout = "The last checked projection, alternate failure, or trimmed frame lost its owner.";

    internal static Task Run()
    {
        byte[] original = Image(compact: false);
        if (Rewrite(original) is not { } compact || !compact.SequenceEqual(Image(compact: true))
            || Rewrite(Image(compact: false, alternateEntry: true)) is not null
            || Rewrite(Image(compact: false, carrierStillLive: true)) is not null
            || Rewrite(compact) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool compact, bool alternateEntry = false, bool carrierStillLive = false)
    {
        int width = compact ? 3 : 4;
        int carrier = width - 1;
        int first = compact ? 0 : 1;
        int last = compact ? carrier : 0;
        byte[] initial = compact
            ? [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)]
            : [InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2)];
        byte[] alternate = alternateEntry ? [IsTuple, F(3), X(0)] : [];
        byte[] preserve = carrierStillLive ? [Move, Y(carrier), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(width), U(1), .. initial, Move, X(0), Y(carrier),
            .. alternate, IsTaggedTuple, F(4), Y(carrier), U(3), A(2),
            GetTupleElement, Y(carrier), U(1), Y(first),
            GetTupleElement, Y(carrier), U(2), Y(last),
            IsMap, F(3), Y(first), Label, U(3),
            Move, Y(last), X(0), .. preserve,
            Line, U(1), ExternalCall, U(1), U(0),
            Move, X(0), Y(carrier), TrimFrame, U(width - 1), U(1),
            Line, U(2), ExternalCall, U(1), U(1),
            Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(4), Move, Y(carrier), X(0), Deallocate, U(width), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            image
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> record = (start, end, slot) => stackOperands.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            record
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteDisjointFrameLayouts",
            image,
            body,
            stackOperands,
            false,
            false,
            null,
            false,
            null,
            false,
            false,
            true
        );
    }
}

using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTrimmedCallResultOrderChecks
{
    internal const string TestName = "compiler/beam/orders-trimmed-call-result-arguments";
    private const string Pass = "OrderTrimmedCallResultArguments";
    private const string OriginalPass = "OrderIndependentCallArguments";
    private const string HeaderType = "BeamCodeHeader";
    private const string ReaderType = "BeamInstructionReader";
    private const string ReadMethod = "Read";
    private const int FormatVersion = 0;
    private const string PinnedOrder = "Copy the direct result, order the stack loads, and keep them before the trim and call line.";
    private const string DependentMoves = "Keep a move that consumes a freshly written X register in its original dependency order.";
    private const string OverwrittenResult = "Keep the existing moves when an earlier argument overwrites the preceding call result.";
    private const string PartialFrame = "Keep the existing order when the trim retains part of the frame.";
    private const string UntrimmedFrame = "Keep the existing order when the caller has no trim.";
    private const string LineBoundary = "Keep the original move runs separated by their source line.";

    internal static Task Run()
    {
        byte[] result = [Move, X(0), X(1)];
        byte[] firstArgument = [Move, Y(1), X(0)];
        byte[] lastArgument = [Move, Y(0), X(2)];
        byte[] incoming = [.. result, .. firstArgument, .. lastArgument];
        CheckOrder(
            Instructions(incoming, 3),
            Instructions([.. result, .. lastArgument, .. firstArgument], 3)
        );

        byte[] resultToThird = [Move, X(0), X(2)];
        byte[] secondArgument = [Move, Y(2), X(1)];
        byte[] fourthArgument = [Move, Y(0), X(3)];
        CheckOrder(
            Instructions([.. resultToThird, .. secondArgument, .. firstArgument, .. fourthArgument], 4, 3),
            Instructions([.. resultToThird, .. fourthArgument, .. secondArgument, .. firstArgument], 4, 3)
        );

        CheckUnchanged(Instructions([.. result, .. firstArgument, Move, X(1), X(2)], 3), DependentMoves);
        CheckUnchanged(Instructions([.. firstArgument, .. result, .. lastArgument], 3), OverwrittenResult);
        CheckUnchanged(Instructions(
            incoming,
            3,
            removed: 1,
            retained: 1
        ), PartialFrame);
        CheckUnchanged(Instructions(
            incoming,
            3,
            removed: 0,
            retained: 2
        ), UntrimmedFrame);
        CheckUnchanged(Instructions([.. result, Line, U(1), .. firstArgument, .. lastArgument], 3), LineBoundary);

        return Task.CompletedTask;
    }

    private static void CheckOrder(byte[] before, byte[] expected)
    {
        if (Rewrite(before) is not { } after || !after.SequenceEqual(expected))
        {
            throw new InvalidOperationException(PinnedOrder);
        }
    }

    private static void CheckUnchanged(byte[] before, string reason)
    {
        if (Rewrite(before) is not null)
        {
            throw new InvalidOperationException(reason);
        }
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        var compiler = new BeamModuleCompiler("beam_trimmed_call_result_order");
        if (typeof(BeamModuleCompiler).GetMethod(
            Pass,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        ) is null)
        {
            return RewriteOriginal(code);
        }

        return (byte[]?)Call(
            compiler,
            Pass,
            code,
            3
        );
    }

    private static byte[]? RewriteOriginal(ReadOnlyMemory<byte> code)
    {
        object header = New(
            HeaderType,
            FormatVersion,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object instructions = Call(
            Type(ReaderType),
            ReadMethod,
            header,
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            OriginalPass,
            code,
            instructions
        );
    }

    private static byte[] Instructions(
        byte[] moves,
        int arity,
        int removed = 2,
        int retained = 0
    )
    {
        int frame = removed + retained;
        byte[] start = Start(
            1,
            2,
            1,
            frame
        );
        byte[] savedThird = frame == 3 ? [Move, X(2), Y(2)] : [];
        byte[] trim = removed > 0 ? [TrimFrame, U(removed), U(retained)] : [];

        return
        [
            .. start,
            Allocate, U(frame), U(frame),
            Move, X(0), Y(0),
            Move, X(1), Y(1),
            .. savedThird,
            ExternalCall, U(0), U(0),
            .. moves,
            Line, U(2),
            .. trim,
            Line, U(3),
            ExternalCall, U(arity), U(1),
            Deallocate, U(retained),
            Return,
            EndCode
        ];
    }
}

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
    private const int SyntheticAtomCount = 3;
    private const int UnavailableDictionaryImport = -1;
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

        CheckClearedCallResult();
        CheckFramedTailCallResult();
        CheckBoundedTailCounter();
        CheckArithmeticResultOrder();

        return Task.CompletedTask;
    }


    private static void CheckArithmeticResultOrder()
    {
        byte[] original = Image();
        byte[] ordered = Image(literalFirst: true);
        if (RewriteOriginal(original) is not byte[] actual || !actual.SequenceEqual(ordered)
            || RewriteOriginal(ordered) is not null
            || RewriteOriginal(Image(overwritesResult: true)) is not null
            || RewriteOriginal(Image(readsOutgoing: true)) is not null)
        {
            throw new InvalidOperationException(PinnedOrder);
        }

        static byte[] Image(
            bool literalFirst = false,
            bool overwritesResult = false,
            bool readsOutgoing = false
        )
        {
            byte[] receiver = [Move, Y(0), X(0)];
            byte[] constant = [Move, readsOutgoing ? X(0) : I(4), X(overwritesResult ? 1 : 2)];
            byte[] args = literalFirst ? [.. constant, .. receiver] : [.. receiver, .. constant];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    2
                ),
                Allocate, U(1), U(2), Move, X(0), Y(0),
                Line, U(1), GcBifTwo, F(0), U(2), U(0), X(1), I(1), X(1),
                .. args, Line, U(2), TailExternalCallWithFrame, U(3), U(1), U(1), EndCode
            ];
        }
    }

    private static void CheckBoundedTailCounter()
    {
        const int IntegerTypeBits = 0x0020;
        var compiler = new BeamModuleCompiler("tail_counter_fixture");
        int addition = (int)Call(
            compiler,
            "Import",
            "erlang",
            "+",
            2
        )!;
        int type = (int)Call(
            compiler,
            "GetOperandTypeIndex",
            (IntegerTypeBits, (long?)0, (long?)5, (int?)null)
        )!;
        byte[] incoming = Image();
        if (Rewrite(incoming) is not { } actual || !actual.SequenceEqual(Image(direct: true))
            || Rewrite(actual) is not null || Rewrite(Image(duplicate: true)) is not null
            || Rewrite(Image(failingLabel: true)) is not null)
        {
            throw new InvalidOperationException(PinnedOrder);
        }

        byte[]? Rewrite(ReadOnlyMemory<byte> code) =>
            (byte[]?)Call(
                compiler,
                "ScheduleBoundedTailCounter",
                code,
                4
            );

        byte[] Image(bool direct = false, bool duplicate = false, bool failingLabel = false)
        {
            byte[] receiver = [Move, Y(4), X(0)];
            byte[] second = [Move, Y(3), X(1)];
            byte[] literal = [Move, A(1), X(2)];
            byte[] fourth = [Move, Y(2), X(3)];
            byte[] fifth = [Move, duplicate ? Y(2) : Y(1), X(4)];
            byte[] moves = direct
                ? [.. literal, .. fifth, .. fourth, .. second, .. receiver]
                : [Move, X(0), X(5), .. receiver, .. second, .. literal, .. fourth, .. fifth];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    6
                ),
                Allocate, U(5), U(6), Move, X(5), Y(0), Move, X(4), Y(1), Move, X(3), Y(2),
                Move, X(1), Y(3), Move, X(0), Y(4), Line, U(1),
                GcBifTwo, F(failingLabel ? 3 : 0), U(0), U(addition), ExtendedTypedRegisterOperand,
                Y(0), U(type), I(1), X(direct ? 5 : 0), .. moves,
                TailLocalCallWithFrame, U(6), F(2), U(5), Label, U(3), Deallocate, U(5), Return, EndCode
            ];
        }
    }

    private static void CheckFramedTailCallResult()
    {
        byte[] result = [Move, X(0), X(1)];
        byte[] receiver = [Move, Y(0), X(0)];
        byte[] second = [Move, I(2), X(2)];
        byte[] third = [Move, I(3), X(3)];
        byte[] fourth = [Move, I(4), X(4)];
        byte[] fifth = [Move, I(5), X(5)];
        byte[] ordered = Image([.. third, .. second, .. fourth, .. result, .. fifth, .. receiver]);
        if (RewriteOriginal(Image([.. result, .. receiver, .. second, .. third, .. fourth, .. fifth])) is not { } actual
            || !actual.SequenceEqual(ordered)
            || RewriteOriginal(ordered) is not null)
        {
            throw new InvalidOperationException(PinnedOrder);
        }

        byte[] dependent = [Move, X(1), X(4)];
        if (RewriteOriginal(Image([.. result, .. receiver, .. second, .. third, .. dependent, .. fifth])) is not null
            || RewriteOriginal(Image([.. receiver, .. result, .. second, .. third, .. fourth, .. fifth])) is not null)
        {
            throw new InvalidOperationException(DependentMoves);
        }

        static byte[] Image(byte[] moves)
        {
            byte[] start = Start(
                1,
                2,
                2,
                1
            );

            return
            [
                .. start, Allocate, U(1), U(1), Move, X(0), Y(0), ExternalCall, U(1), U(0),
                .. moves, Line, U(1), TailExternalCallWithFrame, U(6), U(1), U(1), EndCode
            ];
        }
    }

    private static void CheckClearedCallResult()
    {
        byte[] result = [Move, X(0), X(2)];
        byte[] receiver = [Move, Y(1), X(0)];
        byte[] independent = [Move, Y(0), X(1)];
        byte[] original = [.. result, .. receiver, .. independent];
        byte[] order = [.. independent, .. result, .. receiver];
        byte[] clears = [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        byte[] partial = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        if (RewriteCleared(Image(original, clears)) is not { } actual
            || !actual.SequenceEqual(Image(order, clears)))
        {
            throw new InvalidOperationException(PinnedOrder);
        }

        if (RewriteCleared(Image(original, partial)) is not null
            || RewriteCleared(Image([.. result, .. receiver, Move, X(0), X(1)], clears)) is not null
            || RewriteCleared(Image([.. result, Line, U(1), .. receiver, .. independent], clears)) is not null
            || RewriteCleared(Image(order, clears)) is not null)
        {
            throw new InvalidOperationException(DependentMoves);
        }

        static byte[] Image(byte[] moves, byte[] kills)
        {
            byte[] start = Start(
                1,
                2,
                2,
                2
            );

            return
            [
                .. start, Allocate, U(2), U(2), Move, X(0), Y(1), Move, X(1), Y(0),
                ExternalCall, U(0), U(0), .. moves, .. kills, Line, U(1), ExternalCall, U(3), U(1),
                Deallocate, U(2), Return, EndCode
            ];
        }

        static byte[]? RewriteCleared(ReadOnlyMemory<byte> code)
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
                SyntheticAtomCount,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                OriginalPass,
                code,
                instructions,
                false,
                true,
                UnavailableDictionaryImport
            );
        }
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
            SyntheticAtomCount,
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

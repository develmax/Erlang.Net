using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDuplicateTrailingSlotChecks
{
    internal const string TestName = "compiler/beam/removes-trailing-duplicate-call-slot";
    private const string OwnedCandidate = "ForwardDuplicatedOwnedFrameReads";
    private const string OwnedCompactor = "CompactDisjointFrames";
    private const string Candidate = "ForwardDuplicatedTrailingFrameReads";
    private const string FrameCompactor = "RemoveUnusedTrailingFrameSlots";
    private const string HeaderType = "BeamCodeHeader";
    private const string ReaderType = "BeamInstructionReader";
    private const string Read = "Read";
    private const string Width = "The returning and tail-call continuation frames must drop the duplicate Y slot.";
    private const string Preserve = "Preserve the live, clobbered or protected stack operand.";
    private const int FormatVersion = 0;

    internal static Task Run()
    {
        byte[] root = [Move, X(0), Y(0)];
        byte[] call = [ExternalCall, U(0), U(0)];
        byte[] reload = [Move, Y(0), X(0)];
        byte[] before = Image(1, [.. call, .. root, BifZero, U(1), X(1), .. reload], tail: true);
        byte[] expected = Image(0, [.. call, BifZero, U(1), X(1)], tail: true);
        Check(before, expected);

        byte[] retained = [Move, X(0), Y(0), .. call];
        byte[] repeated = Image(2,
        [
            .. retained,
            Move, X(0), Y(1),
            BifTwo, F(0), U(1), Y(0), Y(1), X(0),
            Move, X(0), Y(1),
            Move, Y(1), X(1),
            Move, Y(0), X(0)
        ], tail: true);
        byte[] repeatedExpected = Image(1,
        [
            .. retained,
            BifTwo, F(0), U(1), Y(0), X(0), X(0),
            Move, X(0), X(1),
            Move, Y(0), X(0)
        ], tail: true);
        Check(repeated, repeatedExpected);

        Check(
            Image(1, [.. call, .. root, TestHeap, U(2), U(1), PutList, Y(0), A(NilAtomIndex), X(0)]),
            Image(0, [.. call, TestHeap, U(2), U(1), PutList, X(0), A(NilAtomIndex), X(0)])
        );
        CheckUnchanged(Image(1, [.. call, .. root, .. call, .. reload]));
        CheckUnchanged(Image(1, [.. call, .. root, Move, A(1), X(0), .. reload]));
        CheckUnchanged(Image(1, [.. call, .. root, TestHeap, U(2), U(0), PutList, Y(0), A(NilAtomIndex), X(0)]));
        CheckUnchanged(Image(1, [.. call, .. root, GcBifOne, F(0), U(0), U(1), Y(0), X(0)]));
        CheckUnchanged(Image(
            2,
            [
            Move, X(0), Y(1), TryEnter, Y(0), F(3), .. call, Move, Y(1), X(0), TryEnd, Y(0)
        ],
            handler:
        [
            Label, U(3), TryCase, Y(0), Move, Y(1), X(0), Deallocate, U(2), Return
        ]
        ));

        CheckOwnedFrames(call, reload);

        return Task.CompletedTask;
    }

    private static void CheckOwnedFrames(byte[] call, byte[] reload)
    {
        byte[] temporary = [Move, X(0), Y(0)];
        byte[] keep = [Move, X(0), Y(1), .. call, .. temporary];
        byte[] before = Image(2, [.. keep, BifTwo, F(0), U(1), Y(1), Y(0), X(0)]);
        byte[] expected = Image(1, [.. temporary, .. call, BifTwo, F(0), U(1), Y(0), X(0), X(0)]);
        CheckOwned(before, expected);

        byte[] entry = Start(
            1,
            2,
            1,
            1
        );
        byte[] first =
        [
            .. entry, ExactEqualTest, F(3), X(0), A(1)
        ];
        byte[] frame = [Allocate, U(1), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] returning = [Deallocate, U(1), Return];
        byte[] sibling =
        [
            Label, U(3), .. frame, .. temporary, .. call, .. reload, .. returning, EndCode
        ];
        byte[] callOne = [ExternalCall, U(1), U(0)];
        before = [.. first, .. frame, .. temporary, .. reload, .. callOne, .. returning, .. sibling];
        expected = [.. first, Allocate, U(0), U(1), .. callOne, Deallocate, U(0), Return, .. sibling];
        CheckOwned(before, expected);

        byte[] joined =
        [
            .. entry, .. frame, .. temporary, ExactEqualTest, F(3), X(0), A(1),
            Move, A(1), X(0), Label, U(3), .. reload, .. callOne, .. returning, EndCode
        ];
        if (!Rewrite(joined, owned: true).SequenceEqual(joined))
        {
            throw new InvalidOperationException(Preserve);
        }
    }

    private static void CheckOwned(byte[] before, byte[] expected)
    {
        if (!Rewrite(before, owned: true).SequenceEqual(expected))
        {
            throw new InvalidOperationException(Width);
        }
    }

    private static void Check(byte[] before, byte[] expected)
    {
        if (!Rewrite(before).SequenceEqual(expected))
        {
            throw new InvalidOperationException(Width);
        }
    }

    private static void CheckUnchanged(byte[] code)
    {
        if (!Rewrite(code).SequenceEqual(code))
        {
            throw new InvalidOperationException(Preserve);
        }
    }

    private static byte[] Rewrite(byte[] bytes, bool owned = false)
    {
        ReadOnlyMemory<byte> code = bytes;
        var offsets = new List<(int Start, int End, int Slot)>();
        object instructions = Decode(code, (start, end, slot) => offsets.Add((start, end, slot)));
        var compiler = typeof(BeamModuleCompiler);
        string forwarding = owned ? OwnedCandidate : Candidate;
        if (compiler.GetMethod(
            forwarding,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
        ) is not null && Call(
            compiler,
            forwarding,
            code,
            instructions,
            offsets
        ) is byte[] forwarded)
        {
            code = forwarded;
            instructions = Decode(code, null);
        }

        if (owned)
        {
            offsets.Clear();
            instructions = Decode(code, (start, end, slot) => offsets.Add((start, end, slot)));

            return (byte[]?)Call(
                compiler,
                OwnedCompactor,
                code,
                instructions,
                offsets
            ) ?? code.ToArray();
        }

        return (byte[]?)Call(
            compiler,
            FrameCompactor,
            code,
            instructions
        ) ?? code.ToArray();
    }

    private static object Decode(ReadOnlyMemory<byte> code, Action<int, int, int>? stackOffsets)
    {
        object header = New(
            HeaderType,
            FormatVersion,
            MinimumHighestOpcode,
            4,
            1,
            code
        );

        return Call(
            Type(ReaderType),
            Read,
            header,
            2,
            0,
            null,
            stackOffsets
        );
    }

    private static byte[] Image(
        int width,
        byte[] instructions,
        bool tail = false,
        byte[]? handler = null
    )
    {
        byte[] entry = Start(
            1,
            2,
            1,
            1
        );
        byte[] release = tail ? [TailExternalCallWithFrame, U(2), U(2), U(width)] : [Deallocate, U(width), Return];
        byte[] initialization = width == 0 ? [] :
            [InitYRegisters, ExtendedListOperand, U(width), .. Enumerable.Range(0, width).Select(Y)];

        return
        [
            .. entry,
            Allocate, U(width), U(1),
            .. initialization,
            .. instructions,
            .. release,
            .. (handler ?? []),
            EndCode
        ];
    }
}

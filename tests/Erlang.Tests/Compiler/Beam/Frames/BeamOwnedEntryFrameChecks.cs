using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedEntryFrameChecks
{
    internal const string TestName = "compiler/beam/reuses-a-single-frame-after-an-owned-frameless-entry";
    private const string WrongFrame = "Expected one owned frame to shrink while preserving the naked external exit and entry arguments.";
    private const string UnsafeFrame = "A mixed, handled, trimmed or unowned frame must keep its original slots.";
    private const string WrongValue = "The framed branch or the frameless external call changed its result.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Marker = 0x72;
    private const byte Other = 0x82;
    private const byte OwnFailure = 0x15;
    private const byte Naked = 0x35;
    private const byte Joined = 0x45;

    internal static async Task Run()
    {
        byte[] naked = [Label, Three, TailExternalCall, One, Zero];
        byte[] withMove = Body(hasMove: true);
        byte[] withoutMove = Body(hasMove: false);
        byte[] expectedWith = Body(hasMove: true, width: One, second: Y0);
        byte[] expectedWithout = Body(hasMove: false, width: One, second: Y0);
        if (Rewrite(withMove) is not { } moved || !moved.AsSpan().SequenceEqual(expectedWith)
            || Rewrite(withoutMove) is not { } ordinary || !ordinary.AsSpan().SequenceEqual(expectedWithout)
            || Rewrite(withoutMove, useValueLiveness: false) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] mixed = Body(hasMove: true, joinFailure: Naked);
        byte[] framedFailure = Body(hasMove: true, joinFailure: OwnFailure);
        byte[] illegalNakedRead = Body(hasMove: true, nakedBody: [Label, Three, Move, Y0, X0, TailExternalCall, One, Zero]);
        byte[] trimmed = Body(hasMove: true, extra: [TrimFrame, One, One]);
        byte[] handled = Body(hasMove: true, extra: [CatchEnter, Y0, Naked]);
        byte[] unowned = [.. withMove[..^naked.Length], Move, Y1, X0, .. naked];
        if (Rewrite(mixed) is not null || Rewrite(framedFailure) is not null
            || Rewrite(illegalNakedRead) is not null || Rewrite(trimmed) is not null
            || Rewrite(handled) is not null || Rewrite(unowned) is not null)
        {
            throw new InvalidOperationException(UnsafeFrame);
        }

        BeamModule image = BeamModule.Prepare(BeamBinaryHostBytes.Image(moved, labels: 5));
        await using var runtime = new ProcessRuntime();
        Term marker = Term.A(BeamOpcodeEdgeNames.Marker);
        Term other = Term.A(BeamOpcodeEdgeNames.Other);
        Term accepted = Term.A("an_atom");
        ProcessHandle process = runtime.Spawn(async context =>
        {
            if (!Term.Tuple(accepted, marker).Equals(await image.Invoke(context, "run", accepted))
                || !Term.Tuple(other, marker).Equals(await image.Invoke(context, "run", marker))
                || !Term.I(2).Equals(await image.Invoke(context, "run", Term.List(accepted, marker))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Term.A("ok");
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!exit.Equals(Term.A("normal")))
        {
            throw new InvalidOperationException(WrongValue);
        }
    }

    private static byte[] Body(
        bool hasMove,
        byte width = Two,
        byte second = Y1,
        byte joinFailure = Joined,
        byte[]? nakedBody = null,
        byte[]? extra = null
    )
    {
        byte[] setup = hasMove ? [Move, X0, X1] : [];
        byte[] exit = nakedBody ?? [Label, Three, TailExternalCall, One, Zero];

        return
        [
            IsAtom, Naked, X0, .. setup, Allocate, width, hasMove ? Two : One,
            InitYRegisters, ExtendedListOperand, One, second, .. (extra ?? []),
            Move, X0, Y0, Move, Y0, X0, Move, Marker, second,
            ExactEqualTest, joinFailure, X0, Marker, Move, Other, X0, Label, Four,
            TestHeap, Three, One, PutTuple, X0, ExtendedListOperand, Two, X0, second,
            Deallocate, width, Return, .. exit
        ];
    }

    private static byte[]? Rewrite(byte[] body, bool useValueLiveness = true)
    {
        byte[] entry = BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            1
        );
        byte[] encoded = [.. entry, .. body, EndCode];
        ReadOnlyMemory<byte> code = encoded;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            code
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> stack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            9,
            0,
            null,
            stack
        );
        byte[]? rewritten = (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ReuseStackSlots",
            code,
            instructions,
            stackOperands,
            useValueLiveness,
            null
        );

        return rewritten is null ? null : rewritten[entry.Length..^1];
    }
}

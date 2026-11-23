using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTaggedEntryTryFrameChecks
{
    internal const string TestName = "compiler/beam/places-checked-entry-tail-before-try-frame";
    private const string Failure = "A checked entry tail must preserve its fields and the protected fallback input.";
    private const int Fallback = 3;
    private const int Handler = 4;
    private const int LocalInfo = 5;
    private const int LocalEntry = 6;
    private const int LabelCount = LocalEntry + 1;
    private const int Frame = 2;
    private const int Carrier = 0;
    private const int HandlerSlot = 1;
    private const int RecordTag = 7;
    private const int BadValue = 8;
    private const int CallLocation = 1;

    internal static async Task Run()
    {
        byte[] before = Fixture();
        byte[] expected = Fixture(split: true);
        if (Rewrite(before) is not { } after || !after.SequenceEqual(expected)
            || Rewrite(after) is not null
            || Rewrite(Fixture(restoreSlot: HandlerSlot)) is not null
            || Rewrite(Fixture(backEdgeToFallback: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[] runtimeBefore = Fixture(includeLine: false);
        byte[] runtimeAfter = Rewrite(runtimeBefore) ?? throw new InvalidOperationException(Failure);
        var prior = BeamModule.Prepare(Image(runtimeBefore[..^1], labels: LabelCount, functions: 2));
        var moved = BeamModule.Prepare(Image(runtimeAfter[..^1], labels: LabelCount, functions: 2));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A(BeamOpcodeEdgeNames.Marker);
            Term first = Term.I(11);
            Term second = Term.I(22);
            Term bad = Term.A(BeamOpcodeEdgeNames.Other);
            foreach (var module in new[] { prior, moved })
            {
                if (!(await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.Tuple(tag, first, second)))
                    .Equals(Term.Tuple(first, second))
                    || !(await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.List(first, second))).Equals(Term.I(2))
                    || !(await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.Tuple(tag, first))).Equals(bad)
                    || !(await module.Invoke(context, BeamOpcodeEdgeNames.Run, bad)).Equals(bad))
                {
                    throw new InvalidOperationException(Failure);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        var compiler = new BeamModuleCompiler(BeamOpcodeEdgeNames.Module);
        string[] fixtureAtoms =
        [
            BeamOpcodeEdgeNames.Run,
            BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf,
            BeamOpcodeEdgeNames.Erlang,
            BeamOpcodeEdgeNames.Length,
            BeamOpcodeEdgeNames.Marker,
            BeamOpcodeEdgeNames.Other
        ];
        foreach (string atom in fixtureAtoms)
        {
            _ = Call(compiler, "Intern", atom);
        }

        return (byte[]?)Call(
            compiler,
            "PlaceTaggedEntryTailBeforeTryFrame",
            code,
            LabelCount
        );
    }

    private static byte[] Fixture(
        bool split = false,
        int restoreSlot = Carrier,
        bool backEdgeToFallback = false,
        bool includeLine = true
    )
    {
        byte[] frame = [Allocate, U(Frame), U(1)];
        byte[] save = [Move, X(0), Y(Carrier)];
        byte[] accepted = split
            ?
            [
                IsTaggedTuple, F(Fallback), X(0), U(3), A(RecordTag),
                GetTupleElement, X(0), U(1), X(1),
                GetTupleElement, X(0), U(2), X(0), Swap, X(0), X(1),
                TailLocalCall, U(2), F(LocalEntry)
            ]
            :
            [
                .. frame, InitYRegisters, ExtendedListOperand, U(1), Y(HandlerSlot), .. save,
                IsTaggedTuple, F(Fallback), Y(Carrier), U(3), A(RecordTag),
                GetTupleElement, Y(Carrier), U(1), Y(HandlerSlot),
                GetTupleElement, Y(Carrier), U(2), X(1), Move, Y(HandlerSlot), X(0),
                TailLocalCallWithFrame, U(2), F(LocalEntry), U(Frame)
            ];
        byte[] beforeTry = split ? [.. frame, .. save] : [];
        byte[] afterTry = split ? [] : [Move, Y(restoreSlot), X(0)];
        byte[] line = includeLine ? [Line, U(CallLocation)] : [];
        byte[] recovery = backEdgeToFallback ? [Jump, F(Fallback)] : [Move, A(BadValue), X(0), Deallocate, U(Frame), Return];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. accepted, Label, U(Fallback), .. beforeTry,
            TryEnter, Y(HandlerSlot), F(Handler), .. afterTry,
            .. line, ExternalCall, U(1), U(0),
            TryEnd, Y(HandlerSlot), Deallocate, U(Frame), Return,
            Label, U(Handler), TryCase, Y(HandlerSlot), .. recovery,
            .. Start(
                LocalInfo,
                LocalEntry,
                3,
                2
            ),
            TestHeap, U(3), U(2), PutTuple, X(0), ExtendedListOperand, U(2), X(0), X(1),
            Return, EndCode
        ];
    }
}

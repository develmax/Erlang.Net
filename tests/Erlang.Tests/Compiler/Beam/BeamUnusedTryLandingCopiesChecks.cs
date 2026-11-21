using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamUnusedTryLandingCopiesChecks
{
    private const int Handler = 3;
    private const int LabelCount = Handler + 1;
    private const string IncorrectRewrite = "Unused exception landing copies must preserve live handler values and root operands.";

    internal static async Task Check()
    {
        byte[] expected = Fixture(compacted: true);
        byte[] original = Fixture();
        byte[] withClass = Fixture(saveClass: true);
        if (Rewrite(original) is not { } reduced || !reduced.AsSpan().SequenceEqual(expected)
            || Rewrite(withClass) is not { } compactedClass || !compactedClass.AsSpan().SequenceEqual(expected)
            || Rewrite(Fixture(continuation: [BuildStacktrace, Deallocate, U(1), Return])) is not null
            || Rewrite(Fixture(continuation: [Move, Y(0), X(0), BuildStacktrace, Deallocate, U(1), Return])) is not null
            || Rewrite(Fixture(continuation: [TestHeap, U(3), U(1), Move, A(7), X(0), Deallocate, U(1), Return])) is not null)
        {
            throw new InvalidOperationException(IncorrectRewrite);
        }

        var wide = BeamModule.Prepare(Image(original[..^1], labels: LabelCount));
        var narrow = BeamModule.Prepare(Image(reduced[..^1], labels: LabelCount));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.List(Term.I(1), Term.I(2));
            Term badInput = Term.A("not_a_list");
            foreach (BeamModule image in new[] { wide, narrow })
            {
                if (!Term.I(2).Equals(await image.Invoke(context, BeamOpcodeEdgeNames.Run, input))
                    || !Term.A(BeamOpcodeEdgeNames.Marker).Equals(await image.Invoke(
                        context,
                        BeamOpcodeEdgeNames.Run,
                        badInput
                    )))
                {
                    throw new InvalidOperationException(IncorrectRewrite);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[] Fixture(bool compacted = false, bool saveClass = false, byte[]? continuation = null)
    {
        byte[] entry = Start(
            1,
            2,
            2,
            1
        );
        byte[] copies = compacted ? [] :
        [
            .. (saveClass ? new[] { Move, X(0), Y(0) } : []),
            Move, X(1), Y(0), Move, X(2), X(0), Move, X(0), Y(0)
        ];
        continuation ??= [Move, A(7), X(0), Deallocate, U(1), Return];

        return
        [
            .. entry,
            Allocate, U(1), U(1), TryEnter, Y(0), F(Handler),
            ExternalCall, U(1), U(0), TryEnd, Y(0), Deallocate, U(1), Return,
            Label, U(Handler), TryCase, Y(0), .. copies, .. continuation, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> code = source;
        var compiler = new BeamModuleCompiler("beam_unused_try_landing_copies_probe");
        // Keep the synthetic instruction atom operands aligned with BeamOpcodeEdgeBytes.Image.
        string[] fixtureAtoms =
        [
            BeamOpcodeEdgeNames.Run,
            BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf,
            BeamOpcodeEdgeNames.Erlang,
            BeamOpcodeEdgeNames.Length,
            BeamOpcodeEdgeNames.Marker
        ];
        foreach (string name in fixtureAtoms)
        {
            _ = Call(compiler, "Intern", name);
        }

        return (byte[]?)Call(
            compiler,
            "RemoveUnusedTryLandingCopies",
            code,
            LabelCount
        );
    }
}

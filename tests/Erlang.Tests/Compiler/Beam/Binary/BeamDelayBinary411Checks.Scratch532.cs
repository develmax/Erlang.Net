using Erlang;
using static Erlang.Tests.BeamBinaryHostBytes;
using static Erlang.Tests.BeamDelayBinary411Names;

namespace Erlang.Tests;

internal static partial class BeamDelayBinary411Checks
{
    private static async Task DeadOrdinaryScratchBeforeMatcher532(byte[] body)
    {
        BeamModule image = Prepare(body);
        BeamBinaryHostAccess.Require(image.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term selected in new Term[] { Term.A(BeamOpcodeEdgeNames.Other), new BitString([9, 8, 7]) })
        {
            await actor.InvokeAsync(async context =>
            {
                Term retained = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), Term.I(532));
                context.Mailbox.Send(selected);
                context.Mailbox.Send(retained);
                Term observed = await image.Invoke(context, BeamOpcodeEdgeNames.Run, new BitString([1, 2]));
                BeamBinaryHostAccess.Require(observed.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
                BeamBinaryHostAccess.Require(context.Mailbox.Count == 1);
                Term? remaining = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                BeamBinaryHostAccess.Require(retained.Equals(remaining) && context.Mailbox.Count == 0);

                return true;
            });
        }
    }

    private static byte[] OrdinaryMatcher532(byte[] prefix)
        => BeamBinaryHostBytes.Join(
            prefix,
            Instruction(Label, U(3)),
            Instruction(Loop, F(4), X(0)),
            [Remove],
            Instruction(Move, A(7), X(0)),
            [Return],
            Instruction(Label, U(4)),
            Instruction(Wait, F(3))
        );

    private static void CandidateAndLiveScratchRefused532()
    {
        // Live context is still outside the admitted ordinary-scratch subset.
        byte[] context = OrdinaryMatcher532(Start(1, 0, 1));
        BeamBinaryHostAccess.Require(!Prepare(context).Supports(BeamOpcodeEdgeNames.Run, 1));
        // Killing the context does not kill a separately saved opaque position.
        byte[] position = OrdinaryMatcher532(BeamBinaryHostBytes.Join(
            Start(1, 0, 1),
            Instruction(
                SavePosition,
                X(1),
                X(2),
                U(2)
            ),
            Instruction(Move, A(7), X(1))
        ));
        BeamBinaryHostAccess.Require(!Prepare(position).Supports(BeamOpcodeEdgeNames.Run, 1));
        // A TRUE candidate binary start has no adjacent reviewed bs_match pair.
        // Ordinary pre-receive admission must never relax this independent gate.
        byte[] candidate = BeamBinaryHostBytes.Join(
            Instruction(Label, U(3)),
            Instruction(Loop, F(4), X(0)),
            Instruction(
                BeamDelayScratch532Names.LegacyStart,
                F(4),
                X(0),
                U(1),
                X(0)
            ),
            Instruction(Move, A(7), X(1)),
            [Remove],
            Instruction(Move, A(7), X(0)),
            [Return],
            Instruction(Label, U(4)),
            Instruction(Wait, F(3))
        );
        BeamBinaryHostAccess.Require(!Prepare(candidate).Supports(BeamOpcodeEdgeNames.Run, 1));
    }
}

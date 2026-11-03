using Erlang;
using static Erlang.Tests.BeamBinaryHostBytes;
using static Erlang.Tests.BeamDelayBinary411Names;

namespace Erlang.Tests;

internal static partial class BeamDelayBinary411Checks
{
    private static byte[] Y(int index) => [BeamOpcodeEdgeBytes.Y(index)];

    private static byte[] Delay(int label = 3) => BeamBinaryHostBytes.Join(
        Instruction(Label, U(label)),
        Instruction(WaitTimeout, F(label), [BeamOpcodeEdgeBytes.I(0)]),
        [TimeoutOpcode],
        Instruction(Move, A(7), X(0)),
        [Return]
    );

    private static BeamModule Prepare(byte[] body, int arity = 1)
        => BeamModule.Prepare(BeamBinaryHostBytes.Image(body, arity: arity, labels: 7));

    internal static async Task Safe()
    {
        // The two branches are disjoint: binary branch terminates; delay arm
        // has never produced a context. Both execute against the same image.
        BeamModule image = Prepare(
            BeamBinaryHostBytes.Join(
                Instruction(
                    EqualOpcode,
                    F(4),
                    X(0),
                    A(7)
                ),
                Start(2, 1, 1),
                Instruction(Move, A(7), X(0)),
                [Return],
                Instruction(Label, U(4)),
                Delay(5)
            ),
            2
        );
        BeamBinaryHostAccess.Require(image.Supports(BeamOpcodeEdgeNames.Run, 2));
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string selector in new[] { BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other })
        {
            Term result = await actor.InvokeAsync(context => image.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                Term.A(selector),
                new BitString([1, 2])
            ).AsTask());
            BeamBinaryHostAccess.Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        }
    }

    internal static Task Opaque()
    {
        // Deliberately conservative: clearing a context still cannot erase
        // current-function path history before a delay.
        BeamBinaryHostAccess.Require(!Prepare(BeamBinaryHostBytes.Join(Start(1, 0, 1),
            Instruction(Move, A(7), X(1)), Delay())).Supports(BeamOpcodeEdgeNames.Run, 1));
        BeamBinaryHostAccess.Require(!Prepare(BeamBinaryHostBytes.Join(Start(1, 0, 1), Delay())).Supports(BeamOpcodeEdgeNames.Run, 1));
        BeamBinaryHostAccess.Require(!Prepare(BeamBinaryHostBytes.Join(
            Start(1, 0, 1),
            Instruction(
                SavePosition,
                X(1),
                X(2),
                U(2)
            ),
            Instruction(Move, A(7), X(1)),
            Delay()
        )).Supports(BeamOpcodeEdgeNames.Run, 1));
        foreach (bool position in new[] { false, true })
        {
            byte[] stored = position ? Instruction(
                SavePosition,
                X(1),
                Y(0),
                U(2)
            ) : Instruction(Move, X(1), Y(0));
            byte[] body = BeamBinaryHostBytes.Join(
                Instruction(Allocate, U(1), U(1)),
                Instruction(Move, A(7), Y(0)),
                Start(1, 0, 1),
                stored,
                Instruction(Move, A(7), X(1)),
                Instruction(Label, U(3)),
                Instruction(WaitTimeout, F(3), [BeamOpcodeEdgeBytes.I(0)]),
                [TimeoutOpcode],
                Instruction(Move, A(7), X(0)),
                Instruction(Deallocate, U(1)),
                [Return]
            );
            BeamBinaryHostAccess.Require(!Prepare(body).Supports(BeamOpcodeEdgeNames.Run, 1));
        }

        return Task.CompletedTask;
    }

    internal static async Task Join()
    {
        // Both predecessor orders, with Term and genuinely uninitialized
        // siblings. Must masks alone erase Context-vs-uninitialized at a join.
        foreach (bool contextFirst in new[] { false, true })
            foreach (bool initializedSibling in new[] { false, true })
                foreach (bool position in new[] { false, true })
                {
                    byte[] opaque = position
                        ? BeamBinaryHostBytes.Join(Start(2, 1, 2), Instruction(
                            SavePosition,
                            X(2),
                            X(3),
                            U(3)
                        ))
                        : Start(2, 1, 2);
                    byte[] safe = initializedSibling
                        ? Instruction(Move, A(7), X(position ? 3 : 2))
                        : Instruction(Move, A(7), X(0));
                    byte[] body = BeamBinaryHostBytes.Join(
                        Instruction(
                            EqualOpcode,
                            F(4),
                            X(0),
                            A(7)
                        ),
                        contextFirst ? opaque : safe,
                        Instruction(Jump, F(5)),
                        Instruction(Label, U(4)),
                        contextFirst ? safe : opaque,
                        Delay(5)
                    );
                    BeamBinaryHostAccess.Require(!Prepare(body, 2).Supports(BeamOpcodeEdgeNames.Run, 2));
                }
        // First safe visit cannot suppress a later tainted loop/backedge visit.
        byte[] loop = BeamBinaryHostBytes.Join(
            Instruction(Allocate, U(1), U(1)),
            Instruction(Move, X(0), Y(0)),
            Instruction(Label, U(3)),
            Instruction(WaitTimeout, F(3), [BeamOpcodeEdgeBytes.I(0)]),
            [TimeoutOpcode],
            Instruction(Move, Y(0), X(0)),
            Start(1, 0, 1),
            Instruction(Move, A(7), X(1)),
            Instruction(Jump, F(3))
        );
        BeamBinaryHostAccess.Require(!Prepare(loop).Supports(BeamOpcodeEdgeNames.Run, 1));
        // Dead ordinary scratch before a nonbinary matcher is admitted by518.
        // Require actual mailbox execution; candidate binary pairs remain gated.
        byte[] matcher = BeamBinaryHostBytes.Join(
            Start(1, 0, 1),
            Instruction(Move, A(7), X(1)),
            Instruction(Label, U(3)),
            Instruction(Loop, F(4), X(0)),
            [Remove],
            Instruction(Move, A(7), X(0)),
            [Return],
            Instruction(Label, U(4)),
            Instruction(Wait, F(3))
        );
        await DeadOrdinaryScratchBeforeMatcher532(matcher);
        CandidateAndLiveScratchRefused532();
    }
}

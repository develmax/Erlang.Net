using static Erlang.Tests.BeamBinaryHostBytes;
using static Erlang.Tests.BeamOpaqueJoin412Names;

namespace Erlang.Tests;

internal static class BeamOpaqueJoin412Checks
{
    private static byte[] Y(int index) => [BeamOpcodeEdgeBytes.Y(index)];

    private static byte[] Image(bool nativeEscape = false, bool position = false, bool reverse = false)
    {
        byte[] contextArm = Join(
            Instruction(
                StartMatch,
                F(1),
                X(1),
                U(2),
                X(1)
            ),
            position ? Instruction(
                SavePosition,
                X(1),
                Y(0),
                U(2)
            ) : Instruction(Move, X(1), Y(0))
        );
        byte[] termArm = Instruction(Move, X(1), Y(0));

        return BeamBinaryHostBytes.Image(
            Join(
                Instruction(Allocate, U(1), U(2)),
                Instruction(Move, A(7), Y(0)),
                Instruction(
                    EqualOpcode,
                    F(4),
                    X(0),
                    A(7)
                ),
                reverse ? termArm : contextArm,
                Instruction(Jump, F(5)),
                Instruction(Label, U(4)),
                reverse ? contextArm : termArm,
                Instruction(Label, U(5)),
                Instruction(Move, Y(0), X(0)),
                nativeEscape ? Instruction(ExternalCall, U(1), U(0)) : Join(
                Instruction(
                    StartMatch,
                    F(1),
                    X(0),
                    U(1),
                    X(0)
                ), Instruction(
                    Tail,
                    X(0),
                    X(0),
                    U(1)
                )),
                Instruction(Deallocate, U(1)),
                [Return]
            ),
            arity: 2,
            labels: 6
        );
    }

    internal static async Task Transport()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool reverse in new[] { false, true })
        {
            BeamModule module = BeamModule.Prepare(Image(reverse: reverse));
            BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 2));
            foreach (string selector in new[] { BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other })
            {
                BitString source = new([1, 127, 255]);
                Term actual = await actor.InvokeAsync(context => module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    Term.A(selector),
                    source
                ).AsTask());
                BeamBinaryHostAccess.Require(actual.Equals(source));
            }
        }
    }

    internal static Task Refusals()
    {
        foreach (byte[] image in new[] { Image(nativeEscape: true), Image(position: true),
            Image(nativeEscape: true, reverse: true), Image(position: true, reverse: true) })
        {
            bool refused;
            try
            {
                refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 2);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);
        }

        return Task.CompletedTask;
    }

    internal static Task Budget()
    {
        foreach (int bits in new[] { ReviewedVariantBits, ExcessVariantBits })
        {
            var body = new List<byte[]> { Instruction(Allocate, U(bits), U(bits + 1)) };
            for (int index = 0; index < bits; index++)
                body.Add(Instruction(Move, A(7), Y(index)));
            for (int index = 0; index < bits; index++)
            {
                int alternate = 3 + index * 2, join = alternate + 1;
                body.Add(Instruction(
                    EqualOpcode,
                    F(alternate),
                    X(index),
                    A(7)
                ));
                body.Add(Instruction(Move, X(bits), X(bits + 1)));
                body.Add(Instruction(
                    StartMatch,
                    F(1),
                    X(bits + 1),
                    U(bits + 2),
                    X(bits + 1)
                ));
                body.Add(Instruction(Move, X(bits + 1), Y(index)));
                body.Add(Instruction(Jump, F(join)));
                body.Add(Instruction(Label, U(alternate)));
                body.Add(Instruction(Move, X(bits), Y(index)));
                body.Add(Instruction(Label, U(join)));
            }
            body.Add(Instruction(Deallocate, U(bits)));
            body.Add([Return]);
            bool supported;
            try
            {
                supported = BeamModule.Prepare(BeamBinaryHostBytes.Image(Join(body.ToArray()),
                    arity: bits + 1, labels: 3 + bits * 2)).Supports(BeamOpcodeEdgeNames.Run, bits + 1);
            }
            catch (NotSupportedException)
            {
                supported = false;
            }
            BeamBinaryHostAccess.Require(supported == (bits == ReviewedVariantBits));
        }

        return Task.CompletedTask;
    }
}

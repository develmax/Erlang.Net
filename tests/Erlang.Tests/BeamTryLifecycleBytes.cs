using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTryLifecycleBytes
{
    internal static byte[] Image()
    {
        // Synthetic wire program only. This is not an original OTP compatibility witness.
        byte[] code = [.. Start(
            BeamTryLifecycleHostNames.InfoLabel,
            BeamTryLifecycleHostNames.EntryLabel,
            BeamTryLifecycleHostNames.RunAtomIndex,
            BeamTryLifecycleHostNames.RunArity
        ),
            BeamTryLifecycleHostNames.AllocateOpcode, U(BeamTryLifecycleHostNames.FrameSlots), U(BeamTryLifecycleHostNames.RunArity),
            BeamTryLifecycleHostNames.TryOpcode, Y(0), F(BeamTryLifecycleHostNames.HandlerLabel),
            BeamTryLifecycleHostNames.CallFunOpcode, U(BeamTryLifecycleHostNames.FunctionArity),
            BeamTryLifecycleHostNames.TryEndOpcode, Y(0),
            BeamTryLifecycleHostNames.DeallocateOpcode, U(BeamTryLifecycleHostNames.FrameSlots),
            BeamTryLifecycleHostNames.ReturnOpcode,
            BeamTryLifecycleHostNames.LabelOpcode, U(BeamTryLifecycleHostNames.HandlerLabel),
            BeamTryLifecycleHostNames.TryCaseOpcode, Y(0),
            BeamTryLifecycleHostNames.MoveOpcode, A(BeamTryLifecycleHostNames.HandlerMarkerAtomIndex), X(0),
            BeamTryLifecycleHostNames.DeallocateOpcode, U(BeamTryLifecycleHostNames.FrameSlots),
            BeamTryLifecycleHostNames.ReturnOpcode];

        return BeamOpcodeEdgeBytes.Image(code, labels: BeamTryLifecycleHostNames.LabelCount);
    }
}

using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTryExtensionBytes
{
    internal static byte[] Tail(bool outerContinuation)
    {
        byte[] entry = Start(
            BeamTryLifecycleHostNames.InfoLabel,
            BeamTryLifecycleHostNames.EntryLabel,
            BeamTryLifecycleHostNames.RunAtomIndex,
            BeamTryLifecycleHostNames.RunArity
        );
        byte[] tail = [BeamTryLifecycleHostNames.AllocateOpcode, U(0), U(BeamTryLifecycleHostNames.RunArity),
            BeamTryLifecycleHostNames.CallFunOpcode, U(BeamTryLifecycleHostNames.FunctionArity),
            BeamTryLifecycleHostNames.DeallocateOpcode, U(0), BeamTryLifecycleHostNames.ReturnOpcode];
        if (!outerContinuation)
            return BeamOpcodeEdgeBytes.Image([.. entry, .. tail]);
        byte[] code = [.. entry,
            BeamTryLifecycleHostNames.AllocateOpcode, U(0), U(BeamTryLifecycleHostNames.RunArity),
            BeamTryExtensionHostNames.LocalCallOpcode, U(BeamTryLifecycleHostNames.RunArity), F(BeamTryExtensionHostNames.MiddleEntryLabel),
            BeamTryExtensionHostNames.PutTupleOpcode, X(0), BeamTryExtensionHostNames.ExtendedOperand,
            U(BeamTryExtensionHostNames.TupleArity), A(BeamTryLifecycleHostNames.HandlerMarkerAtomIndex), X(0),
            BeamTryLifecycleHostNames.DeallocateOpcode, U(0), BeamTryLifecycleHostNames.ReturnOpcode,
            .. Start(
                BeamTryExtensionHostNames.MiddleInfoLabel,
                BeamTryExtensionHostNames.MiddleEntryLabel,
                BeamTryExtensionHostNames.MiddleAtomIndex,
                BeamTryLifecycleHostNames.RunArity
            ), .. tail];

        return BeamOpcodeEdgeBytes.Image(
            code,
            labels: BeamTryExtensionHostNames.TwoFunctionLabelCount,
            functions: BeamTryExtensionHostNames.FunctionCount
        );
    }
}

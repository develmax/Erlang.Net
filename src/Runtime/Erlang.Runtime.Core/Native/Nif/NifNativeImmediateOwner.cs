namespace Erlang;

internal static class NifNativeImmediateOwner
{
    internal static NifNativeCallEnvironment Resolve(nint environmentHint)
    {
        NifNativeCallbackScope? scope = NifNativeCallbackScope.TryCurrent528;
        // A stale bound invocation must fail before any other valid hint can
        // select a destination. Never rescue an expired native invocation.
        NifNativeCallEnvironment? bound = scope?.InvocationEnvironment is not null
            ? NifNativeCallEnvironment.CurrentInvocation() : null;
        if (NifNativeCallEnvironment.TryResolveImmediate714(environmentHint, out var explicitOwner))
        {
            if (bound is not null && !ReferenceEquals(bound.Runtime, explicitOwner!.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            // A real independent env admits pure immediate construction on its
            // foreign thread without fabricating a process callback scope.
            if (scope is null && !explicitOwner!.IsIndependent)
                throw new InvalidOperationException(NifNativeDiagnostics.NativeThreadUnsupported);

            return explicitOwner!;
        }
        // NULL/unregistered/nonaccessible hints remain nonbinding for immediates
        // only when a live actual invocation provides their owner.
        if (bound is not null) return bound;
        if (scope is null)
            throw new InvalidOperationException(NifNativeDiagnostics.NativeThreadUnsupported);

        return NifNativeCallEnvironment.Resolve(environmentHint);
    }
}

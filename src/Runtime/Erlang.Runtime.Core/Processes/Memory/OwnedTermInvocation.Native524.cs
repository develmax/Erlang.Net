namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    // Async-local admission belongs to this real invocation, including a dirty
    // worker's captured context. Never read the actor's mutable CurrentOwned506.
    internal static Term AdmitNativeResult524(ProcessRuntime runtime, Term value)
    {
        if (!NativeResourceRoots524.Contains524(value)) return value;
        for (OwnedTermInvocation506? invocation = active.Value; invocation is not null; invocation = invocation.previousActive)
            if (!invocation.IsDisposed && ReferenceEquals(invocation.Runtime, runtime))
                return invocation.Frame.Retain(value);
        throw new NotSupportedException(NativeResource524Names.GuestRootsRequired);
    }

    internal static Term AdmitNativeStoredResult524(Term value)
    {
        if (!NativeResourceRoots524.Contains524(value)) return value;
        OwnedTermInvocation506? invocation = Active;
        if (invocation is { IsDisposed: false })
            return AdmitNativeResult524(invocation.Runtime, value);

        return NativeTermHostScope524.AdmitCurrent524(value);
    }
}

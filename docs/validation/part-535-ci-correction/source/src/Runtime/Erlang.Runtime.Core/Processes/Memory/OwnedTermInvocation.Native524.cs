namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    // Async-local admission belongs to this real invocation, including a dirty
    // worker's captured context. Never read the actor's mutable CurrentOwned506.
    internal static Term AdmitNativeResult524(ProcessRuntime runtime, Term value)
    {
        if (!NativeResourceRoots524.Contains524(value) && !ReMagicRoots597.Contains597(value)) return value;
        for (OwnedTermInvocation506? invocation = active.Value; invocation is not null; invocation = invocation.previousActive)
            if (!invocation.IsDisposed && ReferenceEquals(invocation.Runtime, runtime))
                return invocation.Frame.Retain(value);
        throw new NotSupportedException(NativeResource524Names.GuestRootsRequired);
    }

    internal static Term AdmitNativeStoredResult524(Term value)
        => AdmitNativeStoredResultSelected594(value, null);

    internal static Term AdmitNativeStoredResultSelected594(Term value, AtomicsHostCopyTransaction591? transaction)
    {
        if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Counters421 } counters
            && CountersRoots596.Direct596(counters) is { } countersBacking)
            return AdmitRehydratedCountersSelected596(counters, countersBacking, transaction);
        if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Regex411 } regex
            && ReMagicRoots597.Direct597(regex) is { } regexBacking)
            return AdmitRehydratedRegex597(regex, regexBacking, transaction);
        if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 } atomics
            && AtomicsRoots586.Direct586(atomics) is { } backing)
            return AdmitRehydratedAtomicsSelected594(atomics, backing, transaction);
        if (!NativeResourceRoots524.Contains524(value)) return value;
        OwnedTermInvocation506? invocation = Active;
        if (invocation is { IsDisposed: false })
            return AdmitNativeResult524(invocation.Runtime, value);

        return NativeTermHostScope524.AdmitCurrent524(value);
    }
}

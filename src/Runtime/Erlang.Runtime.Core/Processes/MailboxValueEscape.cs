namespace Erlang;

internal static class MailboxValueEscape
{
    internal static bool CanMaterialize(ProcessRuntime runtime, Term value)
    {
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(value);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            if (current is not (Nil or Atom or Integer or FloatTerm or ReferenceTerm
                or Pid or PortTerm or BitString or FunctionTerm or Cons or TupleTerm or MapTerm or NativeRecordTerm))
                return false; // Preserve existing extension/unknown-term transport.
            if (current is FunctionTerm { HasRuntimeLocalTransferDescriptor: true } function
                && !RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, function, out _))
                return false; // No new foreign canonical/opaque authority.
            foreach (var child in new DebugDirtyCopyFrame424(current).Children) pending.Push(child);
            if (current is MapTerm map)
                foreach (var pair in map.ArithmeticKeyEntries)
                {
                    pending.Push(pair.Key);
                    pending.Push(pair.Value);
                }
        }

        return true;
    }

    internal static bool RequiresDetach(Term value)
    {
        if (ResourceHostCopy611.Requires(value)) return true;
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(value);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            if (ProcessTermHeap506.Storage(current) is not null) return true;
            foreach (var child in NativeMapOwnedRoots510.Children(current)) pending.Push(child);
        }

        return false;
    }

    // Caller owns an actual Entry operation or snapshot interest throughout.
    // Pure backed composites MUST copy even when no native Host ticket is needed.
    internal static Term Detach(ProcessRuntime runtime, Term value,
        AtomicsHostCopyTransaction591 transaction)
    {
        Term portable = OwnedTermDetachedCopy506.CopyAtomics586(
            runtime,
            value,
            exportHostAtomics586: true,
            hostCopies591: transaction
        );

        return NativeResourceRoots524.Contains524(portable)
            ? NativeTermHostScope524.AdmitCurrent524(runtime, portable) : portable;
    }

    // Genuine guest admission supplies the actual target frame. Foreign fragment
    // headers are copied/imported; no context capability is invented here.
    internal static Term ImportGuest(ProcessRuntime runtime, ProcessTermFrame506 target, Term backedValue)
    {
        if (target.OwnsMailboxStorage(backedValue))
            return target.AdoptGuestInto507(target.Reserve(), backedValue);
        // AdoptGuestInto507 validates local headers and rejects foreign fragment
        // storage. Detach under the live operation interest, then IMPORT here.
        Term portable = OwnedTermDetachedCopy506.CopyAtomics586(runtime, backedValue,
            exportHostAtomics586: false);
        int slot = target.Reserve();
        target.SetPrepared(slot, portable);

        return target.Read(slot);
    }
}

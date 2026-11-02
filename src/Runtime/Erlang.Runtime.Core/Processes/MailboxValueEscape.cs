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
            if (current is HamtBitmapTerm429 bitmap)
            {
                try
                {
                    runtime.RequireHamtNode429(bitmap);
                }
                catch (ArgumentException)
                {
                    return false;
                }
                foreach (Term child in bitmap.Children) pending.Push(child);
                continue;
            }
            if (current is not (Nil or Atom or Integer or FloatTerm or ReferenceTerm
                or Pid or PortTerm or BitString or FunctionTerm or Cons or TupleTerm or MapTerm or NativeRecordTerm))
                return false; // Preserve existing extension/unknown-term transport.
            if (current is FunctionTerm function
                && !RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, function, out _))
            {
                if (function.HasRuntimeLocalTransferDescriptor)
                    return false; // No new foreign canonical/opaque authority.
                // An ordinary Host callback graph owns no native custody to
                // transfer. Keep its established public object/delegate identity.
                // Resource/native graphs still take their actual copy path.
                if (!RequiresDetach(value)) return false;
            }
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
        => ImportGuestWithLiteral801(
            runtime,
            target,
            backedValue,
            null
        );

    internal static Term ImportGuestWithLiteral801(
        ProcessRuntime runtime,
        ProcessTermFrame506 target,
        Term backedValue,
        ModuleLiteralRoots801? literalRoots801
    )
    {
        if (target.OwnsMailboxStorage(backedValue))
            return target.AdoptGuestInto507(target.Reserve(), backedValue);
        // AdoptGuestInto507 validates local headers and rejects foreign fragment
        // storage. Detach under the live operation interest, then IMPORT here.
        Term portable = OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, backedValue, literalRoots801);
        int slot = target.Reserve();
        target.SetPreparedWithLiteral801(slot, portable, literalRoots801);

        return target.Read(slot);
    }
}

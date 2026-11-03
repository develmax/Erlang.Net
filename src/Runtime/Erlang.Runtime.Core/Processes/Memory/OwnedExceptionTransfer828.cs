namespace Erlang;

// This is only a decision to preserve an already portable exception object.
// It cannot admit guest code, grant a holder or make a native graph portable.
internal static class OwnedExceptionTransfer828
{
    internal static ErlangException Transfer(ProcessRuntime runtime, ErlangException error, Func<Term, Term> transfer)
    {
        if (error.ExplicitStackTrace && NeedsNoTransfer(error.Reason) && NeedsNoTransfer(error.StackTraceTerm))
        {
            runtime.ObserveIncomingTerm(error.Reason);
            runtime.ObserveIncomingTerm(error.StackTraceTerm);

            return error;
        }

        return error.WithTransferredTerms506(transfer);
    }

    private static bool NeedsNoTransfer(Term source)
    {
        try
        {
            // Existing proof rejects native storage BEFORE graph getters and
            // conservatively refuses unknown extensions or native map views.
            _ = PortableControlTerm826.Capture(source);
        }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException)
        {
            return false;
        }

        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            // Metadata only: never read resource payloads, native header words,
            // binary bytes or lazy function identity providers for this choice.
            if (value is ResourceReferenceTerm or HamtBitmapTerm429) return false;
            if (value is BitString binary
                && (binary.ResourceBacking577.NativeStorage524 is not null || binary.ResourceSliceOrigin577 is not null))
                return false;
            if (value is FunctionTerm { StoredLocalIdentity: { } identity }) pending.Push(identity.Unique);
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }

        return true;
    }
}

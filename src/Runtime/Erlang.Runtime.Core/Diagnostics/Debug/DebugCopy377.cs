namespace Erlang;

internal static class DebugCopy377
{
    internal static Term Copy(ProcessContext context, Term source, bool copyLiterals)
    {
        OwnedTermInvocation506? invocation = context.CurrentOwned506;
        if (invocation?.BindingKind507 != OwnedBindingKind507.LocalDebugCopy377)
            return DebugSharingCopy425.Copy(context.Runtime, source, copyLiterals);
        if (invocation.IsDisposed || !invocation.CanBorrow507 || !invocation.OwnsContext507(context)
            || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);
        if (!CanCopyGuest377(context.Runtime, source))
        {
            // Opaque/restored callable recipes retain the old portable input
            // boundary. They never see borrowed native closure environments.
            Term portable = invocation.Export(source);
            Term copied = DebugSharingCopy425.Copy(context.Runtime, portable, copyLiterals);
            invocation.Frame.Set(invocation.ResultSlot507, copied);

            return invocation.Frame.Read(invocation.ResultSlot507);
        }
        var temporary = new AtomicsHostCopyTransaction591();
        Exception? primary = null;
        try
        {
            var destination = new DebugCopyDestination391(context, invocation);
            Term result = DebugSharingCopy425.CopySelected377(
                context.Runtime,
                source,
                copyLiterals,
                temporary,
                destination
            );

            // All temporary typed Host carriers remain alive until real native
            // headers/canonical closure environments acquire their own guest leases.
            return invocation.Frame.AdoptGuestInto507(invocation.ResultSlot507, result);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                temporary.Dispose();
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    private static bool CanCopyGuest377(ProcessRuntime runtime, Term root)
    {
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (value is FunctionTerm function)
            {
                if (!RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, function, out var recipe)) return false;
                if (recipe is not ExternalStoredFunctionRecipe && recipe is not DebugDirtyExternalFunctionRecipe424
                    && !(recipe is ICanonicalGuestRecipe507 canonical && canonical.AdmittedTo507(runtime))) return false;
            }
            else if (value is not (Atom or Nil or Integer or FloatTerm or Cons or TupleTerm or MapTerm
                or HamtBitmapTerm429 or NativeRecordTerm or BitString or ReferenceTerm or Pid or PortTerm)) return false;
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }

        return true;
    }
}

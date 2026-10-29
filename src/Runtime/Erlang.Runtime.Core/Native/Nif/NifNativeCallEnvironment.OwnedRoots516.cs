namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private ProcessTermFrame506? guestRoots516;
    private bool attachedGuestExecution683;
    internal bool UsesOwnedExecution683 => Runtime.OwnedTermsPilot506 || attachedGuestExecution683;
    private HashSet<Term>? retainedGuestIdentities516;
    private HashSet<NifNativeTermNode>? retainedGuestPending516;

    internal void AttachGuestRoots516(ProcessContext caller)
    {
        _ = Pointer;
        if (!ReferenceEquals(Runtime, caller.Runtime))
            throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
        if (IsIndependent)
            throw new InvalidOperationException(NifNativeEnvironmentNames.ProcessBound);
        if (caller.OwnedExecutionEnabled683 && guestRoots516 is null)
        {
            guestRoots516 = caller.TermHeap506.Enter();
            // Captured only from this validated actual process-bound caller.
            attachedGuestExecution683 = true;
        }
    }

    // Native environments are caller-synchronized, as required by the NIF API.
    // There is no environment gate around this traversal or detached copying.
    private void RetainGuestValue516(NifNativeTermValue value)
    {
        RetainNativeValue524(value);
        ProcessTermFrame506? roots = guestRoots516;
        if (roots is null)
            return;
        if (value.StaticTerm is { } existing && retainedGuestIdentities516?.Contains(existing) == true
            || value.Pending is { } existingNode && retainedGuestPending516?.Contains(existingNode) == true)
            return;
        var leaves = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var nodes = new HashSet<NifNativeTermNode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<NifNativeTermValue>();
        bool stablePending = true;
        pending.Push(value);
        while (pending.TryPop(out NifNativeTermValue next))
        {
            if (next.StaticTerm is { } term)
            {
                if (retainedGuestIdentities516?.Contains(term) != true)
                    leaves.Add(term);
            }
            else if (next.Pending is { } node && retainedGuestPending516?.Contains(node) != true && nodes.Add(node))
            {
                stablePending &= node is NifNativeTupleNode or NifNativeConsNode or NifNativeBinaryNode or NifNativeMapNode504;
                for (int index = node.ChildCount - 1; index >= 0; index--)
                    pending.Push(node.Child(index));
            }
        }
        var captured = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        foreach (Term leaf in leaves)
        {
            // A new static composite retains its complete physical graph. Never
            // infer joint custody merely from some already inventoried children.
            bool stable = CaptureImmutableGraph516(leaf, out HashSet<Term> graph);
            roots.RetainLocalGraph507(leaf);
            if (stable)
                captured.UnionWith(graph);
            else
                stablePending = false;
        }
        // Commit metadata ONLY after every required root publication succeeded.
        // A later metadata allocation failure can leave a conservative subset;
        // every recorded identity still has actual live environment custody.
        (retainedGuestIdentities516 ??= new(ReferenceEqualityComparer.Instance)).UnionWith(captured);
        if (stablePending)
            (retainedGuestPending516 ??= new(ReferenceEqualityComparer.Instance)).UnionWith(nodes);
    }

    private static bool CaptureImmutableGraph516(Term source, out HashSet<Term> graph)
    {
        graph = new(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        bool stable = true;
        pending.Push(source);
        while (pending.TryPop(out Term? current))
        {
            if (!graph.Add(current))
                continue;
            stable &= current is Nil or Atom or Integer or FloatTerm or Cons or TupleTerm or MapTerm
                or BitString or NativeRecordTerm or FunctionTerm or HamtBitmapTerm429 or Pid or PortTerm or ResourceReferenceTerm
                || current.GetType() == typeof(ReferenceTerm);
            // Callback-free physical rows/tree/CWT reads; no hash, runtime or
            // native-owner acquisition. Unknown graphs remain uncached.
            foreach (Term child in NativeMapOwnedRoots510.Children(current))
                pending.Push(child);
        }

        return stable;
    }

    private void ClearGuestRootInventory516()
    {
        retainedGuestIdentities516?.Clear();
        retainedGuestPending516?.Clear();
    }

    internal nuint CopyValueFrom516(NifNativeTermValue value)
        => CopyValueFromOwnedSource683(value, false);

    internal nuint CopyValueFromOwnedSource683(NifNativeTermValue value, bool sourceOwnedExecution683)
    {
        if (!UsesOwnedExecution683 && !sourceOwnedExecution683)
            return AddValue(NifNativeTermCopy504.Copy(value));
        // The source environment remains admitted while copying. New wrappers
        // must not retain another process's physical tuple/list cells.
        NifNativeTermValue copied = NifNativeTermCopy504.CopyWithStatic516(
            value,
            term => OwnedTermDetachedCopy506.CopyAtomics586(Runtime, term, exportHostAtomics586: false)
        );

        return AddValue(copied);
    }

    internal Term ExportResult516(nuint handle)
    {
        Term value = Get(handle);

        if (UsesOwnedExecution683 && (NativeResourceRoots524.Contains524(value) || AtomicsRoots586.Contains586(value) || CountersRoots596.Contains596(value) || ReMagicRoots597.Contains597(value) || BinaryProgramRoots598.Contains598(value))
            && OwnedTermInvocation506.TryAdmitNativeGuestResult535(
                Runtime,
                Image,
                value,
                out Term guest
            ))
            return guest;

        if (!UsesOwnedExecution683 && ResourceHostCopy611.Requires(value))
            return AtomicsHostAggregate594.Publish(transaction =>
            {
                // All builtin carriers must roll back if the existing native
                // resource Host admission fails for a composite result.
                Term host = OwnedTermDetachedCopy506.CopyAtomics586(
                    Runtime,
                    value,
                    exportHostAtomics586: true,
                    hostCopies591: transaction
                );

                return NativeResourceRoots524.Contains524(host)
                    ? NativeTermHostScope524.AdmitCurrent524(Runtime, host) : host;
            });

        // Export opaque results while the environment frame still owns every
        // source graph/root and backing.
        Term exported = UsesOwnedExecution683
            ? OwnedTermDetachedCopy506.CopyAtomics586(Runtime, value, exportHostAtomics586: false) : value;
        if (!NativeResourceRoots524.Contains524(exported)) return exported;

        return UsesOwnedExecution683
            ? OwnedTermInvocation506.AdmitNativeResult524(Runtime, exported)
            : NativeTermHostScope524.AdmitCurrent524(Runtime, exported);
    }
}

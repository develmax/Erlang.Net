namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private ProcessTermFrame506? guestRoots516;
    private HashSet<Term>? retainedGuestIdentities516;
    private HashSet<NifNativeTermNode>? retainedGuestPending516;

    internal void AttachGuestRoots516(ProcessContext caller)
    {
        _ = Pointer;
        if (!ReferenceEquals(Runtime, caller.Runtime))
            throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
        if (IsIndependent)
            throw new InvalidOperationException(NifNativeEnvironmentNames.ProcessBound);
        if (Runtime.OwnedTermsPilot506 && guestRoots516 is null)
            guestRoots516 = caller.TermHeap506.Enter();
    }

    // Native environments are caller-synchronized, as required by the NIF API.
    // There is no environment gate around this traversal or detached copying.
    private void RetainGuestValue516(NifNativeTermValue value)
    {
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
    {
        if (!Runtime.OwnedTermsPilot506)
            return AddValue(NifNativeTermCopy504.Copy(value));
        // The source environment remains admitted while copying. New wrappers
        // must not retain another process's physical tuple/list cells.
        NifNativeTermValue copied = NifNativeTermCopy504.CopyWithStatic516(
            value,
            term => OwnedTermDetachedCopy506.Copy(Runtime, term)
        );

        return AddValue(copied);
    }

    internal Term ExportResult516(nuint handle)
    {
        Term value = Get(handle);

        // Dynamic NIF functions remain an opaque boundary. Export while the
        // environment frame still owns every source graph/root and backing.
        return Runtime.OwnedTermsPilot506 ? OwnedTermDetachedCopy506.Copy(Runtime, value) : value;
    }
}

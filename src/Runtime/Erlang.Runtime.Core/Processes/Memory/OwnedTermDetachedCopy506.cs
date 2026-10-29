namespace Erlang;

internal static class OwnedTermDetachedCopy506
{
    internal static Term Copy(ProcessRuntime runtime, Term source)
        => CopyAtomics586(runtime, source);

    internal static Term CopyAtomics586(
        ProcessRuntime runtime,
        Term source,
        bool? exportHostAtomics586 = null,
        AtomicsHostCopyTransaction591? hostCopies591 = null
    )
    {
        bool hostExport = exportHostAtomics586 ?? !runtime.OwnedTermsPilot506;
        bool ownsTransaction = hostCopies591 is null;
        Exception? primary = null;
        try
        {
            return CopyCore593(
                runtime,
                source,
                hostExport,
                ref hostCopies591,
                ownsTransaction
            );
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            if (ownsTransaction)
            {
                try
                {
                    hostCopies591?.Dispose();
                }
                catch (Exception cleanup) when (primary is not null)
                {
                    throw new AggregateException(primary, cleanup);
                }
            }
        }
    }

    private static Term CopyCore593(
        ProcessRuntime runtime,
        Term source,
        bool hostExport,
        ref AtomicsHostCopyTransaction591? hostCopies591,
        bool ownsTransaction
    )
    {
        // The native copy_struct traversal copies each heap occurrence; do not
        // turn repeated boxed children into a shared memoized output graph.
        // Explicit frames avoid CLR recursion for deep proper/improper lists.
        var stack = new Stack<OwnedDetachedCopyFrame510>();
        stack.Push(new(runtime, source));
        while (true)
        {
            OwnedDetachedCopyFrame510 frame = stack.Peek();
            if (frame.Index < frame.Children.Length)
            {
                stack.Push(new(runtime, frame.Children[frame.Index]));
                continue;
            }
            // No allocation and no extra traversal for ordinary non-atomics
            // copies or internal guest/store copies with host export disabled.
            if (hostExport && hostCopies591 is null
                && frame.Source is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 or ReferenceResourceKind.Counters421 or ReferenceResourceKind.Regex411 or ReferenceResourceKind.BinarySearchPattern })
                hostCopies591 = new AtomicsHostCopyTransaction591();
            Term copy = frame.NativeMap is { } nativeMap
                ? nativeMap.Build(frame.Copied) : Build(
                    runtime,
                    frame.Source,
                    frame.Copied,
                    hostExport,
                    hostCopies591
                );
            if (frame.NativeMap is not null && frame.Source is MapTerm { IsOrdinaryLogicalMap517: true } && copy is MapTerm raw)
            {
                // Ordinary maps keep the legacy portable dictionary facade for
                // public/foreign use after VM teardown. Preserve the copied raw
                // topology as metadata for same-VM cursors. Never deduplicate a
                // source whose current key identities now collide.
                var logical = new MapTerm(raw.Entries);
                if (logical.Entries.Count == raw.Entries.Count && PreservesFlatKeyOrder517(raw, logical))
                    copy = NativeMapMaterialization505.PublishLogicalEscape517(raw, logical);
            }
            stack.Pop();
            if (stack.Count == 0)
            {
                runtime.ObserveIncomingTerm(copy);
                if (ownsTransaction)
                    hostCopies591?.CommitWrappers593();

                return copy;
            }
            OwnedDetachedCopyFrame510 parent = stack.Peek();
            parent.Copied[parent.Index++] = copy;
        }

    }

    private static bool PreservesFlatKeyOrder517(MapTerm source, MapTerm logical)
    {
        if (source.FlatKeys426 is not { } sourceKeys)
            return true;
        if (logical.FlatKeys426 is not { } logicalKeys || sourceKeys.Items.Count != logicalKeys.Items.Count)
            return false;
        for (int index = 0; index < sourceKeys.Items.Count; index++)
            if (!ReferenceEquals(sourceKeys.Items[index], logicalKeys.Items[index]))
                return false;

        return true;
    }

    private static Term Build(
        ProcessRuntime runtime,
        Term source,
        Term[] values,
        bool exportHostAtomics586,
        AtomicsHostCopyTransaction591? hostCopies591
    )
    {
        switch (source)
        {
            case Nil:
                return Nil.Value;
            case Atom atom:
                return Term.A(atom.Name);
            case Integer integer:
                return new Integer(integer.Value);
            case FloatTerm number:
                return new FloatTerm(number.Value);
            case Cons:
                return new Cons(values[0], values[1]);
            case TupleTerm { Items.Count: 0 }:
                // OTP's empty tuple is always its global literal.
                return source;
            case TupleTerm:
                return new TupleTerm(values);
            case HamtBitmapTerm429 node:
                return new HamtBitmapTerm429(
                    node.Domain,
                    node.Ownership,
                    node.Bitmap,
                    values
                );
            case MapTerm:
                var physical = Enumerable.Range(0, values.Length / 2)
                    .Select(index => new KeyValuePair<Term, Term>(values[index * 2], values[index * 2 + 1])).ToArray();
                var logical = new MapTerm(physical);

                return logical.Entries.Count == physical.Length ? logical : MapTerm.FromNativeLayout504(physical);
            case NativeRecordTerm record:
                return new NativeRecordTerm(
                    record.Module,
                    record.Name,
                    record.IsExported,
                    record.FieldNames.Select((name, index) => new KeyValuePair<Atom, Term>(name, values[index]))
                );
            case BitString binary when binary.NativeResourceIdentity524 is not null:
                return binary.CloneNativeView524();
            case BitString binary:
                if (binary.OwnedStorage577 is not null || binary.ResourceSliceOrigin577 is not null)
                    return binary.DetachResourceView577();
                PersistentBinarySlice416 slice = binary.CapturePersistentSlice416();

                return slice.Backing is null ? new BitString(binary.ToArray(), binary.BitLength)
                    : slice.Backing.Materialize(slice.BitOffset, slice.BitLength);
            case ResourceReferenceTerm resource:
                if (exportHostAtomics586 && resource.ResourceKind == ReferenceResourceKind.Counters421)
                    return CountersRoots596.HostCopy596(resource, hostCopies591);
                if (exportHostAtomics586 && resource.ResourceKind == ReferenceResourceKind.BinarySearchPattern)
                    return BinaryProgramRoots598.HostCopy598(resource, hostCopies591);
                if (exportHostAtomics586 && resource.ResourceKind == ReferenceResourceKind.Regex411)
                    return ReMagicRoots597.HostCopy597(resource, hostCopies591);
                if (exportHostAtomics586 && resource.ResourceKind == ReferenceResourceKind.Atomics413)
                    return AtomicsRoots586.HostCopy586(resource, hostCopies591);

                // New heap wrapper, same actual authenticated magic resource.
                // Native resource lifetime is held separately by actual root/store leases.
                // Freeze the authoritative actual header/prefix identity while
                // the source borrow is held; never read retired heap words later.
                return new ResourceReferenceTerm(resource.Entry, new ReferenceTerm(resource));
            case Pid pid:
                return pid.LocalIdentity is { } pidOwner ? new Pid(pidOwner, pid.Id) : new Pid(pid.Node, pid.Id, pid.Creation);
            case PortTerm port:
                return port.LocalIdentity is { } portOwner ? new PortTerm(portOwner, port.Id) : new PortTerm(port.Node, port.Id, port.Creation);
            case ReferenceTerm reference:
                return new ReferenceTerm(reference);
            case FunctionTerm function:
                if (RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, function, out IRuntimeStoredFunctionRecipe? recipe))
                {
                    if (recipe is ExternalStoredFunctionRecipe external)
                        recipe = new DebugDirtyExternalFunctionRecipe424(external.Owner, external.Identity);

                    return recipe!.Rebind(values);
                }
                // Managed callbacks have opaque CLR captures. Public arguments
                // and dictionary values are detached before reaching this profile;
                // no guest-owned root capability is public. Keep its own delegate
                // lifetime, rather than pretending it is a portable Erlang closure.
                if (function.LocalIdentity is null && function.Environment.Count == 0)
                    return function;
                throw new NotSupportedException(ProcessTerm506Names.UnknownClosure);
            default:
                throw new NotSupportedException(PersistentArenaNames416.Unsupported);
        }
    }
}

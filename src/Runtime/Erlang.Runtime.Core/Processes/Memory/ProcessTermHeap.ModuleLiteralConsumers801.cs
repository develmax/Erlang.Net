namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private ModuleLiteralRoots801? pendingLiteralRoots801;

    internal ModuleLiteralRoots801[] SnapshotLiteralAuthorities801(ModuleLiteralRoots801? carried = null)
    {
        lock (Gate)
        {
            var result = new HashSet<ModuleLiteralRoots801>(ReferenceEqualityComparer.Instance);
            if (carried is not null) result.Add(carried);
            if (pendingLiteralRoots801 is not null) result.Add(pendingLiteralRoots801);
            foreach (ModuleLiteralRoots801 roots in literalBlockRoots801.Values) result.Add(roots);
            foreach (ProcessTermFrame506 frame in frames)
                foreach (ModuleLiteralRoots801 roots in frame.ModuleLiteralAuthorities801)
                    result.Add(roots);
            var retained = new List<ModuleLiteralRoots801>(result.Count);
            try
            {
                foreach (ModuleLiteralRoots801 roots in result) retained.Add(roots.Retain801());

                return retained.ToArray();
            }
            catch
            {
                foreach (ModuleLiteralRoots801 roots in retained) roots.Dispose();
                throw;
            }
        }
    }

    // Narrow preferred lookup for Capture801: independently retain the exact
    // carried/current admission owners while H prevents admission replacement.
    // No domain metadata alone grants authority and no arena lease is acquired.
    internal ModuleLiteralRoots801[] SnapshotPreferredLiteralAuthorities127(ModuleLiteralRoots801? carried)
    {
        lock (Gate)
        {
            ModuleLiteralRoots801? first = null;
            ModuleLiteralRoots801? second = null;
            try
            {
                first = carried?.Retain801();
                if (pendingLiteralRoots801 is not null && !ReferenceEquals(pendingLiteralRoots801, carried))
                    second = pendingLiteralRoots801.Retain801();

                return first is null ? second is null ? [] : [second]
                    : second is null ? [first] : [first, second];
            }
            catch (Exception primary)
            {
                Exception failure = primary;
                foreach (ModuleLiteralRoots801? retained in new[] { first, second })
                    try
                    {
                        retained?.Dispose();
                    }
                    catch (Exception cleanup)
                    {
                        failure = new AggregateException(failure, cleanup);
                    }
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                throw;
            }
        }
    }

    internal ModuleLiteralRoots801? RetainMatchingLiteralAuthority129(ProcessTermSequence506 block)
        => RetainMatchingAuthority129(block, null);

    internal ModuleLiteralRoots801? RetainMatchingIntrinsicAuthority129(RuntimeEmptyTupleStorage803 empty)
        => RetainMatchingAuthority129(null, empty);

    private ModuleLiteralRoots801? RetainMatchingAuthority129(ProcessTermSequence506? block, RuntimeEmptyTupleStorage803? empty)
    {
        lock (Gate)
        {
            // H stabilizes each actual holder and its immutable parent bundle
            // until the exact matching leaf has its own independent retain.
            ModuleLiteralRoots801? found = pendingLiteralRoots801?.RetainMatchingLeaf129(block, empty);
            if (found is not null) return found;
            foreach (ModuleLiteralRoots801 roots in literalBlockRoots801.Values)
            {
                found = roots.RetainMatchingLeaf129(block, empty);
                if (found is not null) return found;
            }
            foreach (ProcessTermFrame506 frame in frames)
                foreach (ModuleLiteralRoots801 roots in frame.ModuleLiteralAuthorities801)
                {
                    found = roots.RetainMatchingLeaf129(block, empty);
                    if (found is not null) return found;
                }

            return null;
        }
    }

    internal ModuleLiteralAdmission801 EnterLiteralAdmission801(ModuleLiteralRoots801? roots)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        ModuleLiteralRoots801? previous = pendingLiteralRoots801;
        var scope = new ModuleLiteralAdmission801(() =>
        {
            if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            pendingLiteralRoots801 = previous;
        });
        pendingLiteralRoots801 = roots;

        return scope;
    }

    internal bool IsAuthenticatedLiteral801(Term value)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        if (IsAuthenticatedEmptyTuple803(value)) return true;
        if (Storage(value) is not ProcessTermSequence506 block
            || block.Owner is not ProcessTermHeap506 heap
            || heap.LiteralDomain800 is not { IsSealed800: true } domain)
            return false;
        if (!ReferenceEquals(domain.Runtime800, runtime))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (pendingLiteralRoots801?.Owns801(block) == true) return true;
        foreach (ModuleLiteralRoots801 roots in literalBlockRoots801.Values)
            if (roots.Owns801(block)) return true;
        foreach (ProcessTermFrame506 frame in frames)
            foreach (ModuleLiteralRoots801 roots in frame.ModuleLiteralAuthorities801)
                if (roots.Owns801(block)) return true;
        throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
    }

    internal ModuleLiteralRoots801? PendingLiteralRoots801
        => Monitor.IsEntered(Gate) ? pendingLiteralRoots801 : throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
}

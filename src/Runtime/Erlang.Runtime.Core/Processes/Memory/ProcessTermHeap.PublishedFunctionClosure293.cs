namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Exact published function whose constructor captured no literal bundle
    // and whose immutable environment contains only immediate/literal-only
    // leaves. With no literal bundle, no external global entry is hidden by
    // this scan shortcut. This grants no code, copy or collector authority.
    internal bool CanSkipEmptyFunctionEnvironment463(Term value)
    {
        if (value is not FunctionTerm { OwnedFunctionStorage: ProcessTermSequence506 block }
            || !ReferenceEquals(block.Owner, this)) return false;
        lock (Gate)
        {
            _ = IsOwnedLocal(value);

            return block.HasImmutableLiteralOnlyClosure132 && block.HasEmptyLiteralCapture200
                && CanUseImmutableClosure293(value, block);
        }
    }

    // Metadata traversal only. Actual collectors still visit ALL environment
    // cells and retire the real dispatch entry; no callback/image authority.
    internal bool CanUseImmutableClosure293(Term value, ProcessTermSequence506 block)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!ReferenceEquals(block.Owner, this) || !ReferenceEquals(Storage(value), block)) return false;
        if (value is MapTerm or HamtBitmapTerm429) return CanUseOwnedMapClosure302(value, block);
        if (value is Cons or TupleTerm)
            return mapHeaders517.ContainsKey(block.Identity)
                ? CanUseOwnedMapClosure302(value, block) : !block.IsMutableSequenceToken814;
        if (value is not FunctionTerm function || !ReferenceEquals(function.OwnedFunctionStorage, block)) return false;
        // IsOwnedLocal/RequireFunctionEntry authenticate the actual current
        // block/header/dispatch binding before reading its monotonic Published.
        _ = IsOwnedLocal(function);
        ManagedFunctionDispatchEntry entry = RequireFunctionEntry(block, out _);

        return ReferenceEquals(entry.Resolve(), function) && entry.Published;
    }
}

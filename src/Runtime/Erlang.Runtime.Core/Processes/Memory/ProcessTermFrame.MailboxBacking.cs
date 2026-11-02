namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    // Identity-only predicate. Existing AdoptGuestInto507 performs live/local
    // validation before any actual same-process root is published.
    internal bool OwnsMailboxStorage(Term value)
        => ProcessTermHeap506.Storage(value) is { } storage && ReferenceEquals(storage.Owner, owner);
}

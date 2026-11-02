namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private bool TryPublishImmediate157(
        int slot,
        Term value,
        bool detachedCopy,
        bool requireOutsideHeap
    )
    {
        if (requireOutsideHeap && Monitor.IsEntered(owner.Gate)) return false;
        // Check metadata storage BEFORE Integer.Value: a foreign/stale native
        // integer must never be inspected by this path. Use the canonical VM
        // small-integer range, not an arbitrary CLR integer bound.
        if (!ImmediateTermRoot161.IsEmpty(value)) return false;
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            if (!owner.IsRegisteredOpenImmediateFrame157(this)) return false;
            // Generic Set's detached copy creates a fresh Integer; local and
            // prepared publications keep the exact original immediate object.
            Term published = detachedCopy && value is Integer number ? new Integer(number.Value)
                : value is Nil ? Nil.Value : value;
            // Preserve real root replacement, old backing/sidecar retirement,
            // binary/native Prepare/Commit/rollback and lease bookkeeping.
            PublishRoot512(slot, published);

            return true;
        }
    }
}

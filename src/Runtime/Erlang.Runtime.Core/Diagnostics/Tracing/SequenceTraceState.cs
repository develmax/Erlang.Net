namespace Erlang;

// Mutation is serialized by SequenceTracing's runtime gate. Native preparation
// for arbitrary terms is an explicit outside-R step, never a property side effect.
internal sealed class SequenceTraceState(ProcessContext context)
{
    private SequenceTraceStorage? storage;
    private PreparedSequenceTraceTransport641? pending;
    private bool closed;

    public SequenceTraceToken? Token
    {
        get => storage?.Token ?? pending?.Token;
        set
        {
            if (value is null)
            {
                Clear();

                return;
            }
            ObjectDisposedException.ThrowIf(closed, this);
            if (storage is not null)
            {
                storage.RewriteMetadata(value);

                return;
            }
            if (pending is not null)
            {
                if (!ReferenceEquals(value.Label, pending.Token.Label))
                    throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
                PreparedSequenceTraceTransport641 replacement = pending.ForkMetadata(
                    value.Flags,
                    value.Serial,
                    value.Sender,
                    value.LastCount
                );
                pending.Dispose();
                pending = replacement;

                return;
            }
            // EnsureToken's default tuple has only immediate fields. This is
            // the only creation admitted synchronously under the runtime gate.
            if (value.Label is not Integer integer || !ProcessTermHeap506.IsImmediateInteger559(integer) ||
                value.Sender is not Pid pid || !ReferenceEquals(pid.LocalIdentity, context.Runtime.LocalIdentity))
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            storage = SequenceTraceStorage.CreateLocal(context, value);
        }
    }

    public ulong Clock { get; set; }
    public ulong LastCount { get; set; }
    internal PreparedSequenceTraceTransport641? Pending => pending;

    internal void InstallPrepared(SequenceTraceStorage replacement)
    {
        ObjectDisposedException.ThrowIf(closed, this);
        SequenceTraceStorage? old = storage;
        PreparedSequenceTraceTransport641? oldPending = pending;
        storage = replacement;
        pending = null;
        old?.Dispose();
        oldPending?.Dispose();
    }

    // Called under R only before this child executes any language operation.
    // Custody is independent of the parent frame and parent Done lifecycle.
    internal void InstallPending(PreparedSequenceTraceTransport641 prepared)
    {
        ObjectDisposedException.ThrowIf(closed, this);
        if (context.ExecutionStarted || !ReferenceEquals(prepared.Payload641.Runtime641, context.Runtime))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        SequenceTraceToken token = prepared.Token;
        PreparedSequenceTraceTransport641 replacement = prepared.ForkMetadata(
            token.Flags,
            token.Serial,
            token.Sender,
            token.LastCount
        );
        Clear();
        pending = replacement;
    }

    internal ProcessTermBorrow506? BorrowPhysical() => storage?.Borrow();

    private void Clear()
    {
        SequenceTraceStorage? old = storage;
        PreparedSequenceTraceTransport641? oldPending = pending;
        storage = null;
        pending = null;
        old?.Dispose();
        oldPending?.Dispose();
    }

    internal void Close()
    {
        if (closed) return;
        closed = true;
        Clear();
    }
}

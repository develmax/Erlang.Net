namespace Erlang;

// Real typed roots are held by exactly one runtime-owned release item. This
// snapshot only distributes independent atomic interests in that ownership.
// No native retain/release callback runs on the publication/forwarding path.
internal sealed class MailboxGraphSnapshot593 : IDisposable, IAsyncDisposable
{
    private readonly MailboxPayloadLease593 roots;
    private int references = 1;
    private int producerReleased;
    private MailboxPayloadLease593? producerLast;
    internal ProcessRuntime Runtime { get; }

    internal MailboxGraphSnapshot593(ProcessRuntime runtime, MailboxPayloadLease593 roots)
    {
        Runtime = runtime;
        this.roots = roots;
    }

    internal IDisposable Retain()
    {
        var lease = new Reference(this);
        int old = Volatile.Read(ref references);
        while (true)
        {
            if (old <= 0)
                throw new ObjectDisposedException(nameof(MailboxGraphSnapshot593));

            int next = checked(old + 1);
            int actual = Interlocked.CompareExchange(ref references, next, old);
            if (actual == old)
                return lease;

            old = actual;
        }
    }

    // Emergency latch-only fallback. Normal transport finally must await
    // DisposeAsync so its final drop settles the exact typed-root work.
    public void Dispose() => ReleaseProducer();

    public ValueTask DisposeAsync()
        => MailboxRootSettlement593.SettleAsync(ReleaseProducer());

    private MailboxPayloadLease593? ReleaseProducer()
    {
        if (Interlocked.CompareExchange(ref producerReleased, 1, 0) == 0)
        {
            try
            {
                producerLast = Release();

                return producerLast;
            }
            finally
            {
                Volatile.Write(ref producerReleased, 2);
            }
        }

        var spin = new SpinWait();
        while (Volatile.Read(ref producerReleased) == 1)
            spin.SpinOnce();

        return Volatile.Read(ref producerLast);
    }

    private MailboxPayloadLease593? Release()
    {
        int left = Interlocked.Decrement(ref references);
        if (left < 0)
            throw new InvalidOperationException(MailboxCustody593Names.NativeUnderflow);

        if (left != 0)
            return null;

        roots.ReleaseOwner();

        return roots;
    }

    private sealed class Reference(MailboxGraphSnapshot593 owner) : IMailboxRootInterest593
    {
        private MailboxGraphSnapshot593? current = owner;

        // Pre-publication rollback may run under a runtime monitor and only
        // schedules. Its producer remains held until outside-monitor finally.
        public void Dispose() => ReleaseLast();

        public MailboxPayloadLease593? ReleaseLast()
            => Interlocked.Exchange(ref current, null)?.Release();
    }
}

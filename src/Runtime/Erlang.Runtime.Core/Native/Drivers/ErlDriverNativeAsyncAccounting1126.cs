using System.Runtime.InteropServices;

namespace Erlang;

// Child-local prototype owner for the new ready/accounted native pool only.
// Existing420 pool explicit-close semantics cannot safely lend its raw handle.
internal sealed class ErlDriverNativeAsyncAccounting1126 : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly ErlDriverServiceFault420 fault;
    private readonly ErlDriverAsyncNativeTypes420.Complete completed;
    private readonly ErlDriverAsyncNativeTypes420.Submit submit;
    private readonly ErlDriverAsyncNativeTypes420.Close close;
    private readonly ErlDriverAsyncAccountingNative1126.Control control;
    private readonly ErlDriverAsyncAccountingNative1126.Finish finish;
    [ThreadStatic] private static ErlDriverNativeAsyncAccounting1126? completionOwner;
    private readonly TaskCompletionSource<bool> closingRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private GCHandle self;
    private nint pool;
    private int leases, calls;
    private bool closing, bridgeRetained;

    internal uint Count { get; }
    internal ulong Frequency { get; }
    internal ulong RawQpcFrequency { get; }
    internal Task Completion => retirement;

    internal ErlDriverNativeAsyncAccounting1126(
        ErlDriverThreadBridge420 bridge,
        uint count,
        Action<ulong> onCompleted,
        ErlDriverServiceFault420 fault
    )
    {
        ArgumentNullException.ThrowIfNull(onCompleted);
        if (count == 0 || count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(count));
        this.bridge = bridge;
        this.fault = fault;
        Count = count;
        completed = identity =>
        {
            // This private scope is installed only by our actual native pool's
            // reverse completion callback. It grants no execution authority.
            var previous = completionOwner;
            completionOwner = this;
            try
            {
                onCompleted(identity);
            }
            catch (Exception failure)
            {
                fault.Record(failure);
            }
            finally
            {
                completionOwner = previous;
            }
        };
        ErlDriverAsyncNativeTypes420.Close? nativeClose = null;
        try
        {
            bridge.DangerousAddRef(ref bridgeRetained);
            nint library = bridge.DangerousGetHandle();
            var create = Export<ErlDriverAsyncNativeTypes420.Create>(library, ErlDriverAsyncAccountingNames1126.Create);
            var info = Export<ErlDriverAsyncAccountingNative1126.Info>(library, ErlDriverAsyncAccountingNames1126.Info);
            submit = Export<ErlDriverAsyncNativeTypes420.Submit>(library, ErlDriverAsyncNames420.Submit);
            nativeClose = close = Export<ErlDriverAsyncNativeTypes420.Close>(library, ErlDriverAsyncNames420.Close);
            control = Export<ErlDriverAsyncAccountingNative1126.Control>(library, ErlDriverAsyncAccountingNames1126.Control);
            finish = Export<ErlDriverAsyncAccountingNative1126.Finish>(library, ErlDriverAsyncAccountingNames1126.Finish);
            var qpcInfo = Export<ErlDriverAsyncAccountingNative1126.QpcInfo>(library, ErlDriverAsyncAccountingNames1126.QpcInfo);
            bool suppress = !ExecutionContext.IsFlowSuppressed();
            AsyncFlowControl flow = default;
            try
            {
                if (suppress) flow = ExecutionContext.SuppressFlow();
                retirement = Retire();
                if (retirement.IsFaulted) retirement.GetAwaiter().GetResult();
            }
            finally
            {
                if (suppress) flow.Undo();
            }
            // Real self-root retains reverse callback and DLL custody until
            // physical join. Forgotten close leaks safely rather than dangling.
            self = GCHandle.Alloc(this);
            Require(create(count, completed, out pool));
            Require(info(pool, out uint actual, out ulong frequency));
            if (actual != count || frequency != ErlDriverAsyncAccountingNames1126.PublicFrequency || Marshal.SizeOf<ErlDriverAsyncAccountingNative1126.Row>() != 88)
                throw new InvalidDataException(ErlDriverAsyncAccountingNames1126.Invalid);
            Frequency = frequency;
            Require(qpcInfo(pool, out ulong qpcFrequency));
            if (qpcFrequency == 0 || qpcFrequency > uint.MaxValue)
                throw new InvalidDataException(ErlDriverAsyncAccountingNames1126.Invalid);
            RawQpcFrequency = qpcFrequency;

        }
        catch (Exception primary)
        {
            closingRequested.TrySetResult(false); // Producer never published; retirement must not close it.
            int failure = pool != 0 && nativeClose is not null ? nativeClose(pool) : 0;
            if (failure == 0)
            {
                pool = 0;
                if (self.IsAllocated) self.Free();
                if (bridgeRetained)
                {
                    bridgeRetained = false;
                    bridge.DangerousRelease();
                }
            }
            else
            {
                var cleanup = new System.ComponentModel.Win32Exception(failure);
                fault.Record(cleanup);
                // Unknown native liveness: retain self/DLL until child exit.
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private static T Export<T>(nint library, string name) where T : Delegate
        => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    private static void Require(int error)
    {
        if (error != 0) throw new System.ComponentModel.Win32Exception(error);
    }

    internal Lease Acquire()
    {
        var lease = new Lease(this); // Allocation precedes visible admission.
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            leases = checked(leases + 1);
            lease.Admitted = true;
        }

        return lease;
    }

    private void Enter(Lease lease)
    {
        lock (gate)
        {
            if (!ReferenceEquals(lease.Owner, this) || !lease.Admitted)
                throw new ObjectDisposedException(nameof(Lease));
            calls = checked(calls + 1);
        }
    }

    private void Leave()
    {
        lock (gate)
        {
            calls--;
            SignalDrained();
        }
    }

    private void SignalDrained()
    {
        if (closing && leases == 0 && calls == 0) drained.TrySetResult();
    }

    private ErlDriverAsyncAccountingSnapshot1126[] Apply(
        Lease lease,
        ErlDriverAsyncAccountingAction1126 action
    )
    {
        if ((uint)action > (uint)ErlDriverAsyncAccountingAction1126.Gather) throw new ArgumentOutOfRangeException(nameof(action));
        ErlDriverAsyncAccountingSnapshot1126[] rows = action == ErlDriverAsyncAccountingAction1126.Gather
            ? new ErlDriverAsyncAccountingSnapshot1126[checked((int)Count)] : [];
        for (int index = 0; index < rows.Length; index++) rows[index] = new();
        Enter(lease);
        try
        {
            for (uint remaining = Count; remaining != 0;)
            {
                uint identity = --remaining;
                Require(control(
                    pool,
                    (uint)action,
                    identity,
                    out var row
                ));
                if (row.Version != ErlDriverAsyncAccountingNames1126.Version || row.Identity != identity
                    || row.OperatingSystemThread == 0 || row.Frequency != Frequency
                    || row.State >= ErlDriverAsyncAccountingNames1126.States)
                    throw new InvalidDataException(ErlDriverAsyncAccountingNames1126.Invalid);
                if (rows.Length != 0) rows[checked((int)identity)].Bind(row);
            }

            return rows;
        }
        finally
        {
            Leave();
        }
    }

    private void Finish(Lease lease, ErlDriverAsyncAccountingAction1126 action)
    {
        if ((uint)action > (uint)ErlDriverAsyncAccountingAction1126.Gather) throw new ArgumentOutOfRangeException(nameof(action));
        Enter(lease);
        try
        {
            Require(finish(pool, (uint)action));
        }
        finally
        {
            Leave();
        }
    }

    private void Submit(
        Lease lease,
        uint lane,
        ulong owner,
        ulong identity,
        nint invoke,
        nint data
    )
    {
        Enter(lease);
        try
        {
            Require(submit(
                pool,
                lane,
                owner,
                identity,
                invoke,
                data
            ));
        }
        finally
        {
            Leave();
        }
    }

    internal Task CloseAsync()
    {
        // Reject both first and repeated close from the actual completion
        // callback, including when another thread already requested retirement.
        if (ReferenceEquals(completionOwner, this))
            throw new InvalidOperationException(ErlDriverAsyncAccountingNames1126.ReentrantClose);
        lock (gate)
        {
            closing = true;
            SignalDrained();
            closingRequested.TrySetResult(true);
        }

        return retirement;
    }

    private async Task Retire()
    {
        if (!await closingRequested.Task.ConfigureAwait(false)) return;
        await drained.Task.ConfigureAwait(false);
        int error = close(pool); // Real jobs drain and every OS worker joins.
        if (error != 0)
        {
            var failure = new System.ComponentModel.Win32Exception(error);
            fault.Record(failure);
            throw failure; // Keep self/DLL: actual liveness is not established.
        }
        pool = 0;
        GC.KeepAlive(completed);
        if (self.IsAllocated) self.Free();
        if (bridgeRetained)
        {
            bridgeRetained = false;
            bridge.DangerousRelease();
        }
    }

    public ValueTask DisposeAsync() => new(CloseAsync());

    internal sealed class Lease : IDisposable
    {
        internal ErlDriverNativeAsyncAccounting1126? Owner;
        internal bool Admitted;

        internal Lease(ErlDriverNativeAsyncAccounting1126 owner) => Owner = owner;

        private ErlDriverNativeAsyncAccounting1126 RequireOwner()
            => Volatile.Read(ref Owner) ?? throw new ObjectDisposedException(nameof(Lease));

        internal ErlDriverAsyncAccountingSnapshot1126[] Apply(ErlDriverAsyncAccountingAction1126 action)
            => RequireOwner().Apply(this, action);

        internal void Finish(ErlDriverAsyncAccountingAction1126 action) => RequireOwner().Finish(this, action);

        internal void Submit(
            uint lane,
            ulong owner,
            ulong identity,
            nint invoke,
            nint data
        )
            => RequireOwner().Submit(
                this,
                lane,
                owner,
                identity,
                invoke,
                data
            );

        public void Dispose()
        {
            var owner = Volatile.Read(ref Owner);
            if (owner is null) return;
            lock (owner.gate)
            {
                if (!ReferenceEquals(Owner, owner)) return;
                if (Admitted)
                {
                    Admitted = false;
                    owner.leases--;
                }
                Volatile.Write(ref Owner, null);
                owner.SignalDrained();
            }
        }
    }
}

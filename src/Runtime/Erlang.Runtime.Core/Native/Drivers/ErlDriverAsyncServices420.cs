using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverAsyncServices420
{
    private readonly object gate = new();
    private readonly Dictionary<int, ErlDriverAsyncJob420> pending = new();
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly ErlDriverTermTokens420 tokens;
    private readonly ErlDriverPortJobs420 jobs;
    private readonly ErlDriverPdlServices420 pdl;
    private readonly ErlDriverServiceFault420 fault;
    private readonly ErlDriverNativeAsyncPool420? pool;
    private int next;
    private bool closing;

    internal ErlDriverAsyncServices420(
        Func<nint, ErlDriverPortReadLease420?> acquire,
        ErlDriverThreadBridge420 bridge,
        ErlDriverTermTokens420 tokens,
        uint asyncThreads,
        ErlDriverPortJobs420 jobs,
        ErlDriverPdlServices420 pdl,
        ErlDriverServiceFault420 fault
    )
    {
        this.acquire = acquire;
        this.bridge = bridge;
        this.tokens = tokens;
        this.jobs = jobs;
        this.pdl = pdl;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_async_port_key)] = new ErlDriverCallbackTypes420.driver_async_port_key(PortKey),
            [nameof(ErlDriverCallbackTypes420.driver_async)] = new ErlDriverCallbackTypes420.driver_async(Submit)
        };
        if (asyncThreads != 0) pool = new(
            bridge,
            asyncThreads,
            Completed,
            fault
        );
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }
    internal ErlDriverNativeAsyncPool420? NativeOwner => pool;

    private uint PortKey(nint port)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return 0;

            return unchecked((uint)((PortTerm)tokens.Resolve(lease.Port.PortTerm)).Id);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return 0;
        }
    }

    private static ErlDriverAsyncNativeTypes420.Work Work(nint pointer)
        => Marshal.GetDelegateForFunctionPointer<ErlDriverAsyncNativeTypes420.Work>(pointer);

    private int Submit(
        nint port,
        nint key,
        nint invoke,
        nint data,
        nint free
    )
    {
        ErlDriverImageLease420? imageLease = null;
        ErlDriverPdl420? heldPdl = null;
        ErlDriverAsyncJob420? job = null;
        bool accepted = false;
        try
        {
            using var lease = acquire(port);
            if (lease is null || invoke == nint.Zero) return -1;
            ErlDriverPort420 owner = lease.Port;
            var invoker = Work(invoke);
            var freer = free == nint.Zero ? null : Work(free);
            lock (gate) ObjectDisposedException.ThrowIf(closing, this);
            if (pool is null)
            {
                if (key != nint.Zero) Marshal.WriteInt32(key, 0);
                invoker(data);
                bool delivered;
                try
                {
                    delivered = owner.ReadyAsync(data, ErlDriverThreadRole420.IsSchedulerThread,
                                       ErlDriverPortTurn420.CallerFor(port, 0));
                }
                catch (ErlDriverSourceBadArgument420)
                {
                    delivered = false;
                }
                if (!delivered) freer?.Invoke(data);

                return 0;
            }
            imageLease = owner.AcquireImageWork();
            heldPdl = pdl.RetainForAsync(port);
            uint lane;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, this);
                int identity = next == int.MaxValue ? 1 : next + 1;
                while (pending.ContainsKey(identity)) identity = identity == int.MaxValue ? 1 : identity + 1;
                next = identity;
                lane = key == nint.Zero ? (uint)identity % pool.Lanes
                    : unchecked((uint)Marshal.ReadInt32(key)) % pool.Lanes;
                if (key != nint.Zero) Marshal.WriteInt32(key, unchecked((int)lane));
                job = new(
                    identity,
                    owner,
                    data,
                    freer,
                    imageLease,
                    heldPdl
                );
                pending.Add(identity, job);
                ulong nativeOwner = bridge.ReadOwner();
                // Keep native acceptance and shutdown admission under one lock.
                // A worker completion may wait this lock, but submit never waits it.
                pool.Submit(
                    lane,
                    nativeOwner,
                    checked((ulong)identity),
                    invoke,
                    data
                );
                accepted = true;
            }
            imageLease = null;
            heldPdl = null;

            return job.Identity;
        }
        catch (Exception error)
        {
            if (job is not null && !accepted)
                lock (gate) pending.Remove(job.Identity);
            fault.Record(error);

            return -1;
        }
        finally
        {
            if (!accepted)
            {
                try
                {
                    heldPdl?.Release();
                }
                catch (Exception error)
                {
                    fault.Record(error);
                }
                try
                {
                    imageLease?.Dispose();
                }
                catch (Exception error)
                {
                    fault.Record(error);
                }
            }
        }
    }

    private void Completed(ulong identity)
    {
        ErlDriverAsyncJob420 job;
        lock (gate)
        {
            if (identity > int.MaxValue || !pending.TryGetValue((int)identity, out job!) || job.Completed)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            job.Completed = true;
        }
        try
        {
            _ = jobs.Post(
                job.Port.Identity,
                ErlDriverPortTaskKind420.ReadyAsync,
                scheduler =>
            {
                bool delivered;
                try
                {
                    delivered = job.Port.ReadyAsync(job.Data, scheduler);
                }
                catch (ErlDriverSourceBadArgument420)
                {
                    delivered = false;
                }
                if (delivered) Cleanup(job, false);

                return delivered;
            },
                () => Cleanup(job, true)
            );
        }
        catch (Exception error)
        {
            // Fatal admission failure is preserved, but cannot strand accepted
            // native data/image/PDL ownership behind an unfinishable Done task.
            fault.Record(error);
            Cleanup(job, true);
        }
    }

    private void Cleanup(ErlDriverAsyncJob420 job, bool freeData)
    {
        if (Interlocked.Exchange(ref job.Cleaned, 1) != 0) return;
        var failures = new List<Exception>();
        try
        {
            if (freeData && job.Free is not null)
            {
                using var owner = job.Port.EnterImageOwner();
                job.Free(job.Data);
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            job.Pdl?.Release();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            job.Image.Dispose();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        lock (gate)
        {
            if (failures.Count == 0) job.Done.TrySetResult();
            else job.Done.TrySetException(new AggregateException(failures));
            pending.Remove(job.Identity);
        }
        foreach (var failure in failures) fault.Record(failure);
    }

    internal async Task CloseAsync()
    {
        lock (gate) closing = true;
        if (pool is not null) await pool.CloseAsync().ConfigureAwait(false);
        Task[] completions;
        lock (gate) completions = pending.Values.Select(job => job.Done.Task).ToArray();
        await Task.WhenAll(completions).ConfigureAwait(false);
        if (fault.Failure is { } failure) throw new IOException(ErlDriverHostNames420.Closed, failure);
    }
}

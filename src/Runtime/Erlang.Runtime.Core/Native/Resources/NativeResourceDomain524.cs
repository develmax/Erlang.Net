namespace Erlang;

internal sealed partial class NativeResourceDomain524
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private readonly Dictionary<nint, NativeResourcePayload524> live = new();
    private readonly List<NativeResourceType524> types = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly Thread worker;
    private readonly NativeResourceDispatcherLease536 normalDispatcher536;
    private readonly TaskCompletionSource quiesced = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private NativeResourcePayload524? head, tail, failed;
    private Exception? dispatchFailure;
    private bool closing, workerStarted, workerJoined;
    private long created, destroyed, bytes;
    internal Task Completion { get; }

    internal NativeResourceDomain524(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        worker = new(Run524) { IsBackground = true, Name = NativeResource524Names.Worker };
        normalDispatcher536 = runtime.AcquireNativeResourceDispatcher536();
        try
        {
            Completion = DirtyNativeOwnerFlow419.Start(Join524);
            if (Completion.IsFaulted) Completion.GetAwaiter().GetResult();
            DirtyNativeOwnerFlow419.StartThread(worker);
            workerStarted = true;
        }
        catch (Exception error)
        {
            dispatchFailure = error;
            quiesced.TrySetResult();
            normalDispatcher536.Dispose();
            throw;
        }
    }

    internal NativeResourceType524 OpenDestructorOnly524(
        NifNativeResourceImage524 image,
        string name,
        nint destructor,
        nint stop = 0,
        nint down = 0,
        nint dynamicCall = 0
    )
    {
        if (!ReferenceEquals(runtime, image.Runtime))
            throw new InvalidOperationException(NativeResource524Names.Foreign);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, NativeResource524Names.Closed);
            var type = new NativeResourceType524(
                name,
                image,
                destructor,
                stop,
                down,
                dynamicCall
            );
            try
            {
                types.Add(type);
            }
            catch
            {
                type.Dispose();
                throw;
            }

            return type;
        }
    }

    internal NativeResourcePayload524 Allocate524(NativeResourceType524 type, nuint size)
    {
        if (!ReferenceEquals(type.Runtime, runtime))
            throw new InvalidOperationException(NativeResource524Names.Foreign);
        // Lock order is registry-read -> domain -> type -> image. A writer must
        // never be awaited while this domain gate is held: an existing callback
        // reader may itself need that gate to allocate a resource.
        using NativeResourceAllocationEpoch527 epoch = type.EnterAllocationEpoch527();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, NativeResource524Names.Closed);
            // Resource allocation during load/upgrade is disallowed by the real
            // image pin's Ready admission, matching the pinned source precondition.
            live.EnsureCapacity(checked(live.Count + 1));
            long nextCount = checked(created + 1), nextBytes = checked(bytes + NativeResourcePayload524.AllocationSize524(size));
            var payload = new NativeResourcePayload524(this, type, size);
            try
            {
                live.Add(payload.IdentityPointer524, payload);
                NativeResourceAddresses524.Register524(payload);
                payload.InitializeNativeIdentity527(runtime.ReferenceResources.MakeNativeResource524(payload));
                created = nextCount;
                bytes = nextBytes;
            }
            catch
            {
                // Pointer never escaped and userdata was never initialized by C.
                // Roll back allocation without invoking a destructor on garbage.
                live.Remove(payload.IdentityPointer524);
                runtime.ReferenceResources.ForgetNativeResource524(payload);
                NativeResourceAddresses524.Forget524(payload);
                payload.AbandonUnpublished524();
                throw;
            }

            return payload;
        }
    }

    internal BitString Binary524(NativeResourcePayload524 payload, nint data, nuint length)
        => BitString.FromNativeStorage524(new NativeResourceBinaryStorage524(payload, data, length));

    internal void Enqueue524(NativeResourcePayload524 payload, bool runOnNormalScheduler = false)
    {
        try
        {
            lock (gate)
            {
                payload.NormalSchedulerRetirement536 = runOnNormalScheduler;
                if (tail is null) head = payload;
                else tail.NextRetirement524 = payload;
                tail = payload;
                signal.Release();
            }
        }
        catch (Exception error)
        {
            // Intrusive node/payload/type/image remain strongly retained.
            Interlocked.CompareExchange(ref dispatchFailure, error, null);
        }
    }

    internal void Completed524(NativeResourcePayload524 payload)
    {
        runtime.ReferenceResources.ForgetNativeResource524(payload);
        lock (gate)
        {
            live.Remove(payload.IdentityPointer524);
            bytes -= payload.AllocationBytes;
            destroyed++;
            if (payload.Failure524 is not null)
            {
                payload.NextFailure524 = failed;
                failed = payload;
            }
            if (closing && live.Count == 0)
                signal.Release();
        }
    }

    internal Task BeginClose524()
    {
        NativeResourceTypeTable527? registry;
        lock (gate)
        {
            if (closing) return Completion;
            closing = true;
            registry = resourceTypes527;
        }
        // A native callback can allocate under gate. Never wait for the registry
        // writer while holding that domain gate.
        try
        {
            registry?.BeginClose527();
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref dispatchFailure, error, null);
        }
        lock (gate)
        {
            closePrepared527 = true;
            signal.Release();
        }

        return Completion;
    }

    internal NativeResourceDomainSnapshot524 Snapshot524()
    {
        lock (gate)
        {
            int pending = 0;
            for (NativeResourcePayload524? node = head; node is not null; node = node.NextRetirement524)
                pending++;

            return new(
                created,
                destroyed,
                bytes,
                live.Count,
                pending,
                closing,
                worker.IsAlive,
                workerJoined,
                Completion.IsCompletedSuccessfully,
                dispatchFailure ?? failed?.Failure524
            );
        }
    }

    private void Run524()
    {
        using var cpu = runtime.Statistics.UserCpu.Enter();
        using var microstate = runtime.Statistics.Microstates.Attach(RuntimeMicrostateNames.Auxiliary, RuntimeMicrostate.Sleep);
        try
        {
            while (true)
            {
                signal.Wait();
                NativeResourcePayload524? payload;
                lock (gate)
                {
                    payload = head;
                    if (payload is not null)
                    {
                        head = payload.NextRetirement524;
                        payload.NextRetirement524 = null;
                        if (head is null) tail = null;
                    }
                    else if (closePrepared527 && live.Count == 0 && pendingLoads527 == 0)
                        break;
                }
                if (payload is { NormalSchedulerRetirement536: true })
                    normalDispatcher536.Dispatch536(payload.Complete524).GetAwaiter().GetResult();
                else
                {
                    using var state = RuntimeMicrostateExecution.EnterState(runtime.Statistics.Microstates, RuntimeMicrostate.Aux);
                    payload?.Complete524();
                }
            }
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref dispatchFailure, error, null);
        }
        finally
        {
            quiesced.TrySetResult();
        }
    }

    private async Task Join524()
    {
        try
        {
            await quiesced.Task.ConfigureAwait(false);
            if (workerStarted)
                worker.Join();
            workerJoined = true;
            lock (gate)
            {
                if (live.Count != 0 || dispatchFailure is not null)
                    throw new InvalidOperationException(NativeResource524Names.Closed, dispatchFailure);
                foreach (NativeResourceType524 type in types)
                    type.Dispose();
                types.Clear();
            }
            signal.Dispose();
            if (failed?.Failure524 is { } error)
                throw new InvalidOperationException(NativeResource524Names.Retired, error);
        }
        finally
        {
            normalDispatcher536.Dispose();
        }
    }
}

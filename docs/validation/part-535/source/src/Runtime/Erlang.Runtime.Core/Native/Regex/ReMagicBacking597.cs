using System.Runtime.InteropServices;

namespace Erlang;

// One actual magic Binary allocation. No finalizer has guest authority.
internal sealed class ReMagicBacking597 : IThreadPoolWorkItem
{
    private readonly object gate = new();
    private readonly ReMagicOwner597 owner;
    private nint memory;
    private long guest, host;
    private int readers, executing, queueSettled;
    private bool producer = true, closing, scheduled, identityBound, freed;
    private Exception? queueFailure, callbackFailure;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource retired597 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Retired597 => retired597.Task;
    internal ReCode411 CodeOwner597 { get; }
    internal ReMagicBacking597? NextFailure597;
    internal ProcessRuntime Runtime597 => owner.Runtime597;
    internal bool HasGuestRoots597
    {
        get
        {
            lock (gate) return guest != 0;
        }
    }
    internal bool PhysicalFreed597
    {
        get
        {
            lock (gate) return freed;
        }
    }
    internal bool CanExecute597
    {
        get
        {
            lock (gate) return scheduled;
        }
    }

    internal unsafe ReMagicBacking597(ReMagicOwner597 owner, ReCode411 code, nint nativeCode)
    {
        if (IntPtr.Size != sizeof(ulong) || sizeof(ReMagicHeader597) != ReMagicNames597.AllocationBytes
            || Marshal.OffsetOf<ReMagicHeader597>(nameof(ReMagicHeader597.Code)).ToInt32() != ReMagicNames597.PrefixBytes)
            throw new PlatformNotSupportedException(ReMagicNames597.Invalid);
        this.owner = owner;
        CodeOwner597 = code;
        void* selected = NativeMemory.AllocZeroed((nuint)sizeof(ReMagicHeader597));
        if (selected == null) throw new OutOfMemoryException();
        memory = (nint)selected;
        var header = (ReMagicHeader597*)selected;
        header->Flags = NativeResourceMagic527.Magic;
        header->ApparentSize = (nuint)sizeof(nint);
        header->OriginalSize = (nuint)(sizeof(ReMagicHeader597) - ReMagicNames597.BinaryDataOffset);
        header->Destructor = ReMagicRegistry597.Destructor;
        header->AllocationKind = ReMagicNames597.AllocatorDispatch;
        header->Code = nativeCode;
        try
        {
            ReMagicRegistry597.Add(memory, this);
        }
        catch
        {
            NativeMemory.Free(selected);
            memory = 0;
            throw;
        }
    }

    // Constructor failure before owner registration: caller still owns PCRE code.
    internal unsafe void AbandonPrefix597()
    {
        nint selected;
        lock (gate)
        {
            if (!producer || guest != 0 || host != 0 || readers != 0 || identityBound)
                throw new InvalidOperationException(ReMagicNames597.Invalid);
            selected = memory;
            memory = 0;
            freed = true;
        }
        ReMagicRegistry597.Remove(selected, this);
        NativeMemory.Free((void*)selected);
    }

    internal unsafe nint CodePointer597
    {
        get
        {
            lock (gate)
            {
                RequirePrefix597();

                return ((ReMagicHeader597*)memory)->Code;
            }
        }
    }

    internal unsafe void BindIdentity597(IReadOnlyList<uint> words)
    {
        lock (gate)
        {
            RequirePrefix597();
            if (identityBound || words.Count != ReMagicNames597.NumberCount
                || (words[1] & ReferenceNumberLayout.MagicMarker) == 0)
                throw new InvalidOperationException(ReMagicNames597.Invalid);
            var h = (ReMagicHeader597*)memory;
            h->Ref0 = words[0];
            h->Ref1 = words[1];
            h->Ref2 = words[2];
            identityBound = true;
        }
    }

    internal unsafe ReMagicLease597 Retain(ReMagicLeaseKind597 kind)
    {
        if (kind == ReMagicLeaseKind597.Guest)
        {
            return owner.RetainGuest599(this);
        }
        var lease = new ReMagicLease597(this, kind);
        lock (gate)
        {
            RequirePrefix597();
            ObjectDisposedException.ThrowIf(closing || scheduled || !identityBound, ReMagicNames597.Closed);
            if (kind != ReMagicLeaseKind597.Host) throw new ArgumentOutOfRangeException(nameof(kind));
            long nextHost = checked(host + 1);
            long physical = checked(guest + nextHost);
            host = nextHost;
            ((ReMagicHeader597*)memory)->RefCount = physical;
        }

        return lease;
    }

    // Owner admission gate -> backing gate is the sole Guest admission order.
    internal unsafe void CommitGuest599()
    {
        owner.RequireAdmissionGate599();
        lock (gate)
        {
            RequirePrefix597();
            ObjectDisposedException.ThrowIf(closing || scheduled || !identityBound, ReMagicNames597.Closed);
            long nextGuest = checked(guest + 1);
            long physical = checked(nextGuest + host);
            guest = nextGuest;
            ((ReMagicHeader597*)memory)->RefCount = physical;
        }
    }

    internal void RetainReader597()
    {
        lock (gate)
        {
            RequirePrefix597();
            ObjectDisposedException.ThrowIf(closing || scheduled || (!producer && guest == 0 && host == 0), ReMagicNames597.Closed);
            readers = checked(readers + 1);
        }
    }

    internal void ReleaseReader597()
    {
        bool queue;
        lock (gate)
        {
            if (readers == 0) throw new InvalidOperationException(ReMagicNames597.Underflow);
            readers--;
            queue = MakeReady597();
        }
        if (queue) Schedule597();
    }

    internal void ReleaseProducer597()
    {
        bool queue;
        lock (gate)
        {
            if (!producer) return;
            producer = false;
            queue = MakeReady597();
        }
        if (queue) Schedule597();
    }

    internal unsafe void Release(ReMagicLeaseKind597 kind)
    {
        bool queue;
        lock (gate)
        {
            if (kind == ReMagicLeaseKind597.Guest)
            {
                if (guest == 0) throw new InvalidOperationException(ReMagicNames597.Underflow);
                guest--;
            }
            else if (kind == ReMagicLeaseKind597.Host)
            {
                if (host == 0) throw new InvalidOperationException(ReMagicNames597.Underflow);
                host--;
            }
            else throw new ArgumentOutOfRangeException(nameof(kind));
            if (memory != 0) ((ReMagicHeader597*)memory)->RefCount = checked(guest + host);
            queue = MakeReady597();
        }
        if (queue) Schedule597();
    }

    internal nint Binary597(ReMagicLease597 lease)
    {
        lock (gate)
        {
            RequireLease597(lease);

            return memory;
        }
    }

    internal unsafe IReadOnlyList<uint> Numbers597(ReMagicLease597 lease)
    {
        lock (gate)
        {
            RequireLease597(lease);
            var h = (ReMagicHeader597*)memory;

            return new uint[] { h->Ref0, h->Ref1, h->Ref2 };
        }
    }

    internal void CloseVm597()
    {
        bool queue;
        lock (gate)
        {
            closing = true;
            queue = MakeReady597();
        }
        if (queue) Schedule597();
    }

    internal async Task Join597()
    {
        await ready.Task.ConfigureAwait(false);
        Execute(); // Specific backing fallback, outside all runtime/heap/owner gates.
        await Retired597.ConfigureAwait(false);
    }

    private bool MakeReady597()
    {
        if (scheduled || freed || producer || readers != 0 || guest != 0 || (!closing && host != 0)) return false;
        scheduled = true;
        ready.TrySetResult();

        return true;
    }

    private void Schedule597()
    {
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false))
                throw new InvalidOperationException(ReMagicNames597.QueueFailure);
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref queueFailure, error, null);
        }
        finally
        {
            Volatile.Write(ref queueSettled, 1);
        }
    }

    public void Execute()
    {
        if (!CanExecute597) return;
        var wait = new SpinWait();
        while (Volatile.Read(ref queueSettled) == 0) wait.SpinOnce();
        if (Interlocked.CompareExchange(ref executing, 1, 0) != 0) return;
        Exception? failure = null;
        try
        {
            owner.Dispatch597(DestroyPrefix597).GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            failure = error;
        }
        Exception? queueError = Volatile.Read(ref queueFailure);
        if (queueError is not null)
        {
            if (failure is null) failure = queueError;
            else
                try
                {
                    failure = new AggregateException(queueError, failure);
                }
                catch (Exception aggregation)
                {
                    failure = aggregation;
                }
        }
        try
        {
            if (PhysicalFreed597) owner.Finished597(this, failure);
        }
        catch (Exception error)
        {
            if (failure is null) failure = error;
            else
                try
                {
                    failure = new AggregateException(failure, error);
                }
                catch (Exception aggregation)
                {
                    failure = aggregation;
                }
        }
        if (failure is null) retired597.TrySetResult(); else retired597.TrySetException(failure);
    }

    private unsafe void DestroyPrefix597()
    {
        nint selected;
        lock (gate)
        {
            RequirePrefix597();
            if (!scheduled || producer || guest != 0 || readers != 0 || (!closing && host != 0))
                throw new InvalidOperationException(ReMagicNames597.Invalid);
            selected = memory;
        }
        int status = ((delegate* unmanaged[Cdecl]<nint, int>)ReMagicRegistry597.Destructor)(selected);
        if (status != 1) throw new InvalidOperationException(ReMagicNames597.DestructorFailure, callbackFailure);
        ReMagicRegistry597.Remove(selected, this);
        lock (gate)
        {
            memory = 0;
            freed = true;
        }
        NativeMemory.Free((void*)selected);
    }

    internal void RecordDestructorFailure597(Exception error) => Interlocked.CompareExchange(ref callbackFailure, error, null);

    internal unsafe int InvokeActualCodeDestructor(nint selected)
    {
        try
        {
            nint code;
            lock (gate)
            {
                RequirePrefix597();
                if (memory != selected || !scheduled || readers != 0 || guest != 0 || producer)
                    throw new InvalidOperationException(ReMagicNames597.Invalid);
                code = ((ReMagicHeader597*)memory)->Code;
            }
            // Exact source-admitted PCRE2 free, outside every monitor.
            owner.FreeNativeCode597(code);
            lock (gate) ((ReMagicHeader597*)memory)->Code = 0;

            return 1;
        }
        catch (Exception error)
        {
            callbackFailure = error;

            return 0;
        }
    }

    private void RequireLease597(ReMagicLease597 lease)
    {
        if (!lease.Owns(this)) throw new ObjectDisposedException(nameof(ReMagicLease597));
        RequirePrefix597();
        if (!identityBound) throw new InvalidOperationException(ReMagicNames597.Invalid);
    }

    private unsafe void RequirePrefix597()
    {
        if (memory == 0 || freed) throw new ObjectDisposedException(ReMagicNames597.Closed);
        var h = (ReMagicHeader597*)memory;
        if (h->Flags != NativeResourceMagic527.Magic || h->Destructor != ReMagicRegistry597.Destructor
            || h->ApparentSize != (nuint)sizeof(nint) || h->OriginalSize != (nuint)(sizeof(ReMagicHeader597) - ReMagicNames597.BinaryDataOffset)
            || h->AllocationKind != ReMagicNames597.AllocatorDispatch || h->Code == 0
            || h->RefCount != checked(guest + host))
            throw new InvalidOperationException(ReMagicNames597.Invalid);
    }
}

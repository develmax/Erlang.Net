using System.Runtime.InteropServices;

namespace Erlang;

// Actual child-side callback data and image lease, one per real VM port. Driver
// data is opaque even when NULL. Stop is deferred until the outer callback
// returns, so driver_failure cannot free data from under a nested output call.
internal sealed unsafe partial class ErlDriverPort420 : IDisposable
{
    private readonly ErlDriverImage420 image;
    private readonly ErlDriverEntry420 entry;
    private readonly ErlDriverImageLease420 imageLease;
    private readonly ErlDriverPrimitiveAccess420 memory;
    private readonly ErlDriverAbiLayout420 layout457;
    private readonly ErlDriverStartEntry420.Invoke start;
    private readonly Action<ErlDriverPort420> onStopped;
    private readonly Action<ErlDriverPort420, string, nint> missingReady;
    private readonly object callbackGate;
    private readonly object serviceGate = new();
    private int services;
    private bool serviceClosing;
    private int callbackDepth;
    private bool started;
    private bool stopRequested;
    private bool stopped;
    private nint data;
    private readonly TaskCompletionSource? construction455;
    private bool constructing455;

    internal ErlDriverPort420(
        nint nativePort,
        ErlDriverPortIdentity420 identity,
        int openFlags,
        bool softEof,
        ErlDriverHostSlot420 slot,
        ErlDriverPrimitiveAccess420 memory,
        ErlDriverAbiLayout420 layout,
        ErlDriverStartEntry420.Invoke start,
        nuint portTerm,
        nuint connected,
        Action<ErlDriverPort420> onStopped,
        Action<ErlDriverPort420, string, nint> missingReady,
        bool constructing455 = false
    )
    {
        this.constructing455 = constructing455;
        if (constructing455) construction455 = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Slot455 = slot;
        NativePort = nativePort;
        Identity = identity;
        OpenFlags = openFlags;
        SoftEof = softEof;
        image = slot.Image;
        entry = slot.Entry;
        this.memory = memory;
        layout457 = layout;
        this.start = start;
        PortTerm = portTerm;
        Connected = connected;
        this.onStopped = onStopped;
        this.missingReady = missingReady;
        callbackGate = (entry.Flags & ErlDriverPortNames420.PortLockingFlag) != 0 ? new object() : slot.DriverGate;
        imageLease = image.Acquire();
        try
        {
            Queue = new(memory, layout);
        }
        catch
        {
            imageLease.Dispose();
            throw;
        }
    }

    internal ErlDriverHostSlot420 Slot455 { get; }
    internal Task Construction455 => construction455?.Task ?? Task.CompletedTask;

    // Called only after parent commit. No allocations, IPC or throwing cleanup
    // may follow this transfer of the source caller's opaque data pointer.
    internal void AdoptData455(nint callerOwnedData)
    {
        data = callerOwnedData;
        started = true;
    }

    internal void ReleaseConstruction455()
    {
        lock (callbackGate)
        {
            if (!constructing455) return;
            constructing455 = false;
            construction455!.TrySetResult();
            if (stopRequested && callbackDepth == 0) StopCore();
        }
    }

    internal nint NativePort { get; }
    internal ErlDriverPortIdentity420 Identity { get; }
    internal int OpenFlags { get; }
    internal bool SoftEof { get; }
    internal bool Closing => Volatile.Read(ref stopRequested);
    internal nuint PortTerm { get; }
    internal nuint Connected { get; set; }
    internal int ControlFlags { get; set; }
    internal ErlDriverQueue420 Queue { get; }
    internal ErlDriverEntry420 Entry => entry;
    internal bool Stopped
    {
        get
        {
            lock (callbackGate) return stopped;
        }
    }

    internal ErlDriverPortReadLease420? AcquireService()
    {
        lock (serviceGate)
        {
            if (serviceClosing) return null;
            var lease = new ErlDriverPortReadLease420(this);
            services = checked(services + 1);

            return lease;
        }
    }

    internal void ReleaseService()
    {
        lock (serviceGate)
        {
            if (services <= 0) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            services--;
            if (services == 0) Monitor.PulseAll(serviceGate);
        }
    }

    private T Invoke<T>(nuint caller, bool schedulerThread, Func<T> call)
    {
        lock (callbackGate)
        {
            if (stopped || stopRequested) throw new ErlDriverSourceBadArgument420();
            using var owner = image.EnterOwner();
            using var turn = new ErlDriverPortTurn420(NativePort, caller, schedulerThread);
            callbackDepth++;
            try
            {
                return call();
            }
            finally
            {
                callbackDepth--;
                try
                {
                    turn.ReleaseChildren455();
                }
                finally
                {
                    if (stopRequested && callbackDepth == 0 && !constructing455) StopCore();
                }
            }
        }
    }

    internal ErlDriverStartResult420 Start(byte[] command, nuint caller, bool schedulerThread)
        => Invoke(
            caller,
            schedulerThread,
            () =>
        {
            if (started) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            byte[] terminated = new byte[checked(command.Length + 1)];
            command.CopyTo(terminated, 0);
            ErlDriverStartResult420 result;
            fixed (byte* input = terminated)
            {
                nint callback = Entry.Address(ErlDriverEntryNames420.Start);
                // erts_open_driver initializes drv_data=0 and succeeds when
                // no start callback exists. NULL data is not a failure sentinel.
                if (callback == nint.Zero) result = new(nint.Zero, 0);
                else
                {
                    nint value = start(
                        callback,
                        NativePort,
                        (nint)input,
                        out int error
                    );
                    result = new(value, error);
                }
            }
            if (!result.Failed)
            {
                data = result.Data;
                started = true;
            }
            else stopRequested = true;

            return result;
        }
        );

    internal void Output(byte[] bytes, nuint caller, bool schedulerThread)
        => Invoke(
            caller,
            schedulerThread,
            () =>
        {
            if (!started) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            var callback = Entry.Callback<ErlDriverEntryTypes420.Output>(ErlDriverEntryNames420.Output);
            if (callback is not null)
                fixed (byte* input = bytes) callback(data, (nint)input, checked((nuint)bytes.Length));

            // Pinned no_output_callback is a genuine no-op.
            return true;
        }
        );

    private static byte[] Copy(nint pointer, nuint size)
    {
        if (pointer == nint.Zero && size != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[checked((int)size)];
        if (bytes.Length != 0) Marshal.Copy(
            pointer,
            bytes,
            0,
            bytes.Length
        );

        return bytes;
    }

    internal ErlDriverControlResult420 Control(
        uint command,
        byte[] bytes,
        nuint caller,
        bool schedulerThread
    )
        => Invoke(
            caller,
            schedulerThread,
            () =>
        {
            var callback = Entry.Callback<ErlDriverEntryTypes420.Control>(ErlDriverEntryNames420.Control)
                ?? throw new ErlDriverSourceBadArgument420();
            byte* local = stackalloc byte[ErlDriverPortNames420.ControlBufferBytes];
            nint result = (nint)local;
            bool binary = false;
            try
            {
                nint length;
                fixed (byte* input = bytes)
                    length = callback(
                        data,
                        command,
                        (nint)input,
                        checked((nuint)bytes.Length),
                        ref result,
                        ErlDriverPortNames420.ControlBufferBytes
                    );
                binary = (ControlFlags & ErlDriverPortNames420.BinaryControlFlag) != 0;
                if (length < 0) throw new ErlDriverSourceBadArgument420();
                if (result == nint.Zero) return new ErlDriverControlResult420(null, binary);
                bool externalBinary = binary && result != (nint)local;
                nuint size = externalBinary ? memory.BinarySize(result) : checked((nuint)length);
                if (result == (nint)local && size > ErlDriverPortNames420.ControlBufferBytes)
                    throw new ErlDriverSourceBadArgument420();
                nint source = externalBinary ? memory.BinaryBytes(result) : result;

                return new ErlDriverControlResult420(Copy(source, size), binary);
            }
            finally
            {
                if (result != (nint)local && result != nint.Zero)
                {
                    if (binary) memory.ReleaseBinary(result);
                    else memory.Free(result);
                }
            }
        }
        );

    internal byte[] Call(
        uint command,
        byte[] encoded,
        nuint caller,
        bool schedulerThread
    )
        => Invoke(
            caller,
            schedulerThread,
            () =>
        {
            var callback = Entry.Callback<ErlDriverEntryTypes420.Call>(ErlDriverEntryNames420.Call)
                ?? throw new ErlDriverSourceBadArgument420();
            byte* local = stackalloc byte[ErlDriverPortNames420.CallBufferBytes];
            nint result = (nint)local;
            uint flags = 0;
            try
            {
                nint length;
                fixed (byte* input = encoded)
                {
                    length = callback(
                        data,
                        command,
                        (nint)input,
                        checked((nuint)encoded.Length),
                        ref result,
                        ErlDriverPortNames420.CallBufferBytes,
                        ref flags
                    );
                    if (length <= 0 || result == nint.Zero)
                        throw new ErlDriverSourceBadArgument420();
                    if (result == (nint)local && (nuint)length > ErlDriverPortNames420.CallBufferBytes)
                        throw new ErlDriverSourceBadArgument420();
                    // Copy while any input-returned pointer remains pinned.
                    byte[] response = Copy(result, checked((nuint)length));
                    if (response[0] != ErlDriverPortNames420.ExternalTermVersion)
                        throw new ErlDriverSourceBadArgument420();

                    return response;
                }
            }
            finally
            {
                if (result != (nint)local && (flags & ErlDriverPortNames420.KeepCallBufferFlag) == 0)
                    memory.Free(result);
            }
        }
        );

    internal void Ready(string callbackName, nint argument, bool schedulerThread)
        => ReadySelected(
            callbackName,
            argument,
            schedulerThread,
            static () => true
        );

    internal bool ReadySelected(
        string callbackName,
        nint argument,
        bool schedulerThread,
        Func<bool> admit
    )
        => Invoke(
            0,
            schedulerThread,
            () =>
        {
            if (!admit()) return false;
            var callback = Entry.Callback<ErlDriverEntryTypes420.Ready>(callbackName);
            if (callback is null) missingReady(this, callbackName, argument);
            else callback(data, argument);

            return true;
        }
        );

    internal ErlDriverImageLease420 AcquireImageWork() => image.AcquireWorker();

    internal IDisposable EnterImageOwner() => image.EnterOwner();

    internal bool ReadyAsync(nint argument, bool schedulerThread, nuint caller = 0)
        => Invoke(
            caller,
            schedulerThread,
            () =>
        {
            var callback = Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.ReadyAsync);
            if (callback is null) return false;
            callback(data, argument);

            return true;
        }
        );

    internal bool Timeout(bool schedulerThread, Func<bool> admit)
        => Invoke(
            0,
            schedulerThread,
            () =>
        {
            if (!admit()) return false;
            var callback = Entry.Callback<ErlDriverEntryTypes420.Unary>(ErlDriverEntryNames420.Timeout);
            // init_driver installs the genuine no-op timeout fallback.
            callback?.Invoke(data);

            return true;
        }
        );

    internal bool ProcessExited(byte[] prefix, bool schedulerThread, Func<bool> admit)
        => Invoke(
            0,
            schedulerThread,
            () =>
        {
            if (!admit()) return false;
            if (prefix.Length != ErlDriverMonitorLayout420.PrefixBytes)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            var callback = Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.ProcessExit);
            if (callback is null) return false;
            byte* monitor = stackalloc byte[ErlDriverMonitorLayout420.PublicBytes];
            prefix.CopyTo(new Span<byte>(monitor, prefix.Length));
            callback(data, (nint)monitor);

            return true;
        }
        );

    internal void MarkClosing()
    {
        lock (callbackGate) stopRequested = true;
    }

    internal void RequestStop()
    {
        lock (callbackGate)
        {
            if (stopped) return;
            stopRequested = true;
            if (callbackDepth == 0 && !constructing455) StopCore();
        }
    }

    private void StopCore()
    {
        if (stopped || constructing455) return;
        stopped = true;
        lock (serviceGate)
        {
            serviceClosing = true;
            // Thread-safe output/term/queue services do not acquire callbackGate.
            // Their genuine parent admission can finish while close waits here.
            while (services != 0) Monitor.Wait(serviceGate);
        }
        var failures = new List<Exception>();
        try
        {
            if (started)
            {
                using var owner = image.EnterOwner();
                Entry.Callback<ErlDriverEntryTypes420.Stop>(ErlDriverEntryNames420.Stop)?.Invoke(data);
            }
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            Queue.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            imageLease.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            onStopped(this);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    public void Dispose() => RequestStop();
}

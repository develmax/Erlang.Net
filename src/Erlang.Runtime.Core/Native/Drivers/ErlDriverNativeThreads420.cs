using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

// Child-private physical thread observer. The native bridge reserves before
// creating a thread and publishes its genuine OS handle before releasing the
// thread's start gate. A failed native creation aborts an unpublished reservation.
internal sealed class ErlDriverNativeThreads420
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ErlDriverThreadObservation420> workers = new();
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Exception> failures = new();
    private ulong next;
    private bool closed;

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetCurrentProcess")]
    private static extern nint GetCurrentProcess();

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetProcessIdOfThread", SetLastError = true)]
    private static extern uint GetProcessIdOfThread(nint thread);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetThreadId", SetLastError = true)]
    private static extern uint GetThreadId(nint thread);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "DuplicateHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(
        nint sourceProcess,
        nint source,
        nint targetProcess,
        out SafeWaitHandle target,
        uint access,
        [MarshalAs(UnmanagedType.Bool)] bool inherit,
        uint options
    );

    internal ulong Reserve(ErlDriverImage420 image)
    {
        ErlDriverImageLease420 lease = image.AcquireWorker();
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);
                ulong id = checked(next + 1);
                var observation = new ErlDriverThreadObservation420(id, lease);
                workers.Add(id, observation);
                next = id;

                return id;
            }
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    internal void Publish(ulong id, nint nativeHandle)
    {
        // Validate a real thread object, not a caller-provided numeric thread id.
        if (GetThreadId(nativeHandle) == 0 || GetProcessIdOfThread(nativeHandle) != checked((uint)Environment.ProcessId))
            throw new InvalidDataException(ErlDriverThreadNames420.InvalidThread);
        nint process = GetCurrentProcess();
        if (!DuplicateHandle(
            process,
            nativeHandle,
            process,
            out var handle,
            ErlDriverThreadNames420.Synchronize | ErlDriverThreadNames420.QueryLimitedInformation,
            false,
            0
        ))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        ErlDriverThreadWait420? wait = null;
        try
        {
            wait = new(handle);
            lock (gate)
            {
                if (!workers.TryGetValue(id, out var observation) || observation.Thread is not null || observation.Completing)
                    throw new InvalidDataException(ErlDriverThreadNames420.InvalidReservation);
                // Callback takes this same gate, so it cannot reclaim before
                // the actual registration and wait-handle fields are committed.
                observation.Thread = wait;
                try
                {
                    observation.Registration = ThreadPool.UnsafeRegisterWaitForSingleObject(
                        wait,
                        ThreadExited,
                        observation,
                        Timeout.Infinite,
                        executeOnlyOnce: true
                    );
                }
                catch
                {
                    observation.Thread = null;
                    throw;
                }
                wait = null;
            }
        }
        finally
        {
            if (wait is not null) wait.Dispose();
            else if (handle.IsClosed == false)
            {
                // A successful publication transfers handle ownership to the
                // observation. On pre-wrapper allocation failure it remains local.
                lock (gate)
                    if (!workers.TryGetValue(id, out var row) || !ReferenceEquals(row.Thread?.SafeWaitHandle, handle))
                        handle.Dispose();
            }
        }
    }

    internal void AbortUncreated(ulong id)
    {
        ErlDriverThreadObservation420 observation;
        lock (gate)
        {
            if (!workers.TryGetValue(id, out observation!) || observation.Thread is not null || observation.Completing)
                throw new InvalidDataException(ErlDriverThreadNames420.InvalidReservation);
            observation.Completing = true;
        }
        Finish(observation);
    }

    private void ThreadExited(object? state, bool timedOut)
    {
        var observation = (ErlDriverThreadObservation420)state!;
        if (timedOut) return; // Infinite OS wait; never manufacture an exit.
        lock (gate)
        {
            if (!workers.TryGetValue(observation.Id, out var current) || !ReferenceEquals(current, observation) || observation.Completing) return;
            observation.Completing = true;
        }
        Finish(observation);
    }

    private void Finish(ErlDriverThreadObservation420 observation)
    {
        try
        {
            observation.Registration?.Unregister(null);
        }
        catch (Exception error)
        {
            lock (gate) failures.Add(error);
        }
        try
        {
            observation.Thread?.Dispose();
        }
        catch (Exception error)
        {
            lock (gate) failures.Add(error);
        }
        try
        {
            // Windows signals the thread object after full native/CRT TLS
            // teardown, unlike a callback at the end of the driver's function.
            observation.Lease.Dispose();
        }
        catch (Exception error)
        {
            lock (gate) failures.Add(error);
        }
        finally
        {
            lock (gate)
            {
                workers.Remove(observation.Id);
                CompleteDrain();
            }
        }
    }

    internal Task CloseAsync()
    {
        lock (gate)
        {
            closed = true;
            CompleteDrain();

            return drained.Task;
        }
    }

    private void CompleteDrain()
    {
        lock (gate)
            if (closed && workers.Count == 0)
            {
                if (failures.Count == 0) drained.TrySetResult();
                else drained.TrySetException(new AggregateException(failures));
            }
    }
}

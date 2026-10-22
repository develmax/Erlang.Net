namespace Erlang;

internal sealed class ErlDriverSelectServices420
{
    private readonly object gate = new();
    private readonly Dictionary<nint, ErlDriverSelectRow420> events = new();
    private readonly List<Task> retirement = new();
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverPortJobs420 jobs;
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly ErlDriverServiceFault420 fault;
    private bool closing;

    internal ErlDriverSelectServices420(
        Func<nint, ErlDriverPortReadLease420?> acquire,
        ErlDriverPortJobs420 jobs,
        ErlDriverThreadBridge420 bridge,
        ErlDriverServiceFault420 fault
    )
    {
        this.acquire = acquire;
        this.jobs = jobs;
        this.bridge = bridge;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_select)] = new ErlDriverCallbackTypes420.driver_select(Select)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    internal void MissingReady(ErlDriverPort420 port, string callback, nint handle)
    {
        int mode = callback == ErlDriverEntryNames420.ReadyInput ? ErlDriverSelectNames420.Read : ErlDriverSelectNames420.Write;
        if (Select(
            port.NativePort,
            handle,
            mode | ErlDriverSelectNames420.Use | ErlDriverSelectNames420.NoCallback,
            0
        ) != 0)
            throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
        // The parent's original logger transport for missing-ready diagnostics
        // is not integrated in this compile-only slice; that partition is held.
    }

    private int Select(
        nint nativePort,
        nint handle,
        int mode,
        int on
    )
    {
        try
        {
            using var lease = acquire(nativePort);
            if (lease is null) return -1;
            var port = lease.Port;
            ErlDriverImageLease420? read = null, write = null, use = null;
            if (on != 0)
            {
                try
                {
                    if ((mode & ErlDriverSelectNames420.Read) != 0) read = port.AcquireImageWork();
                    if ((mode & ErlDriverSelectNames420.Write) != 0) write = port.AcquireImageWork();
                    if ((mode & ErlDriverSelectNames420.Use) != 0) use = port.AcquireImageWork();
                }
                catch
                {
                    read?.Dispose();
                    write?.Dispose();
                    use?.Dispose();
                    throw;
                }
            }
            try
            {
                bool stopUnknown = false;
                Task? synchronousRetirement = null;
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(closing, this);
                    if (!events.TryGetValue(handle, out var row))
                    {
                        if (on == 0)
                        {
                            stopUnknown = (mode & (ErlDriverSelectNames420.Use | ErlDriverSelectNames420.NoCallback)) == ErlDriverSelectNames420.Use;
                            goto ConfigurationDone;
                        }
                        row = new(handle);
                        events.Add(handle, row);
                    }
                    if (row.StopPending)
                    {
                        bool replaceStop = on != 0 || (mode & (ErlDriverSelectNames420.Use | ErlDriverSelectNames420.NoCallback)) == ErlDriverSelectNames420.Use
                            && row.StopDriver != port.Identity.Driver;
                        if (!replaceStop) return 0;
                        // Source steal_pending_stop_use cancels the old callback
                        // when the fd owner changes its mind or another driver
                        // replaces the pending stop. Never close a reused handle.
                        row.StopPending = false;
                    }
                    bool newRead = on != 0 && (mode & ErlDriverSelectNames420.Read) != 0 && !ReferenceEquals(row.Reader, port);
                    bool newWrite = on != 0 && (mode & ErlDriverSelectNames420.Write) != 0 && !ReferenceEquals(row.Writer, port);
                    if (on != 0 && !newRead && !newWrite) return 0;
                    bool changeRead = (mode & ErlDriverSelectNames420.Read) != 0 && (on == 0 || newRead);
                    bool changeWrite = (mode & ErlDriverSelectNames420.Write) != 0 && (on == 0 || newWrite);
                    var oldWait = row.Wait;
                    if (on == 0 && (mode & ErlDriverSelectNames420.NoCallback) != 0 && oldWait is not null)
                        synchronousRetirement = oldWait.CancelAsync();
                    row.Wait = null;
                    row.Generation = checked(row.Generation + 1);
                    bool stop = on == 0 && (mode & (ErlDriverSelectNames420.Use | ErlDriverSelectNames420.NoCallback)) == ErlDriverSelectNames420.Use;
                    if (stop)
                    {
                        changeRead = true;
                        changeWrite = true;
                    }
                    if (changeRead)
                    {
                        row.ReadGeneration = checked(row.ReadGeneration + 1);
                        row.ReadPending = false;
                        row.ReaderImage?.Dispose();
                        row.Reader = on != 0 ? port : null;
                        row.ReaderImage = read;
                        read = null;
                    }
                    if (changeWrite)
                    {
                        row.WriteGeneration = checked(row.WriteGeneration + 1);
                        row.WritePending = false;
                        row.WriterImage?.Dispose();
                        row.Writer = on != 0 ? port : null;
                        row.WriterImage = write;
                        write = null;
                    }
                    if ((mode & ErlDriverSelectNames420.Use) != 0 && (on == 0 || newRead || newWrite))
                    {
                        row.UserImage?.Dispose();
                        row.User = on != 0 ? port : null;
                        row.UserImage = use;
                        use = null;
                    }
                    row.Pending.RemoveAll(task => task.IsCompletedSuccessfully);
                    retirement.RemoveAll(task => task.IsCompletedSuccessfully);
                    Task[] pending = row.Pending.ToArray();
                    if (stop)
                    {
                        row.StopPending = true;
                        row.StopDriver = port.Identity.Driver;
                    }
                    if (row.Reader is null && row.Writer is null && row.User is null && !row.StopPending) events.Remove(handle);
                    // Stop is deferred until actual wait retirement and already
                    // scheduled ready callbacks settle. Queued old generations
                    // reject admission under the real callback gate.
                    var stopImage = stop ? port.AcquireImageWork() : null;
                    retirement.Add(Reconfigure(
                        row,
                        oldWait,
                        pending,
                        port,
                        stopImage,
                        stop,
                        row.Generation
                    ));
                }
            ConfigurationDone:
                // io.c releases the event-state lock before stop_select.
                synchronousRetirement?.GetAwaiter().GetResult();
                if (stopUnknown) StopNow(port, handle);
            }
            finally
            {
                read?.Dispose();
                write?.Dispose();
                use?.Dispose();
            }

            return 0;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private static void StopNow(ErlDriverPort420 port, nint handle)
    {
        using var image = port.AcquireImageWork();
        using var owner = port.EnterImageOwner();
        port.Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.StopSelect)?.Invoke(handle, 0);
    }

    private async Task Reconfigure(
        ErlDriverSelectRow420 row,
        ErlDriverSelection420? previous,
        Task[] pending,
        ErlDriverPort420 port,
        ErlDriverImageLease420? stopImage,
        bool stop,
        ulong expectedGeneration
    )
    {
        // Force all execution out of Select's native callback before a parent
        // dispatch request; never await the current ready callback from itself.
        await Task.Yield();
        try
        {
            if (previous is not null) await previous.CancelAsync().ConfigureAwait(false);
            await Task.WhenAll(pending).ConfigureAwait(false);
            if (stop)
            {
                await jobs.Post(
                    port.Identity,
                    ErlDriverPortTaskKind420.StopSelect,
                    _ =>
                {
                    lock (gate)
                    {
                        if (!row.StopPending || row.Generation != expectedGeneration) return false;
                        row.StopPending = false;
                        if (row.Reader is null && row.Writer is null && row.User is null
                            && events.TryGetValue(row.Handle, out var registered) && ReferenceEquals(registered, row))
                            events.Remove(row.Handle);
                    }
                    using var owner = port.EnterImageOwner();
                    port.Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.StopSelect)?.Invoke(row.Handle, 0);

                    return true;
                }
                ).ConfigureAwait(false);
            }
            lock (gate)
            {
                if (closing || !events.TryGetValue(row.Handle, out var current) || !ReferenceEquals(current, row)
                    || row.Wait is not null || row.Reader is null && row.Writer is null) return;
                var arm = new ErlDriverSelectArm420(row);
                arm.Wait = new(
                    row.Handle,
                    bridge,
                    () => Signal(arm),
                    fault
                );
                row.Wait = arm.Wait;
            }
        }
        catch (Exception error)
        {
            fault.Record(error);
            throw;
        }
        finally
        {
            stopImage?.Dispose();
        }
    }

    private bool Admit(
        ErlDriverSelectRow420 row,
        ulong generation,
        ErlDriverPort420 port,
        bool write
    )
    {
        lock (gate)
            return !closing && (write ? row.WriteGeneration : row.ReadGeneration) == generation
                && ReferenceEquals(write ? row.Writer : row.Reader, port);
    }

    private void Signal(ErlDriverSelectArm420 arm)
    {
        try
        {
            lock (gate)
            {
                var row = arm.Row;
                if (closing || !events.TryGetValue(row.Handle, out var registered) || !ReferenceEquals(registered, row)) return;
                if (ReferenceEquals(row.Wait, arm.Wait)) row.Wait = null;
                var pending = new List<Task>();
                if (arm.Reader is { } reader && ReferenceEquals(reader, row.Reader)
                    && arm.ReadGeneration == row.ReadGeneration && !row.ReadPending)
                {
                    row.ReadPending = true;
                    pending.Add(ScheduleReady(
                        row,
                        arm.ReadGeneration,
                        reader,
                        false
                    ));
                }
                if (arm.Writer is { } writer && ReferenceEquals(writer, row.Writer)
                    && arm.WriteGeneration == row.WriteGeneration && !row.WritePending)
                {
                    row.WritePending = true;
                    pending.Add(ScheduleReady(
                        row,
                        arm.WriteGeneration,
                        writer,
                        true
                    ));
                }
                row.Pending.RemoveAll(task => task.IsCompletedSuccessfully);
                retirement.RemoveAll(task => task.IsCompletedSuccessfully);
                row.Pending.AddRange(pending);
                var work = Reconfigure(
                    row,
                    arm.Wait,
                    row.Pending.ToArray(),
                    row.Reader ?? row.Writer!,
                    null,
                    false,
                    row.Generation
                );
                retirement.Add(work);
            }
        }
        catch (Exception error)
        {
            fault.Record(error);
        }
    }

    private Task ScheduleReady(
        ErlDriverSelectRow420 row,
        ulong generation,
        ErlDriverPort420 port,
        bool write
    )
    {
        var image = port.AcquireImageWork();

        void Release()
        {
            image.Dispose();
            lock (gate)
            {
                if (write && row.WriteGeneration == generation) row.WritePending = false;
                if (!write && row.ReadGeneration == generation) row.ReadPending = false;
            }
        }
        try
        {
            return jobs.Post(
                port.Identity,
                write ? ErlDriverPortTaskKind420.ReadyOutput : ErlDriverPortTaskKind420.ReadyInput,
                scheduler =>
                {
                    try
                    {
                        return port.ReadySelected(
                            write ? ErlDriverEntryNames420.ReadyOutput : ErlDriverEntryNames420.ReadyInput,
                            row.Handle,
                            scheduler,
                            () => Admit(
                                row,
                                generation,
                                port,
                                write
                            )
                        );
                    }
                    catch (ErlDriverSourceBadArgument420)
                    {
                        return false;
                    }
                    finally
                    {
                        Release();
                    }
                },
                Release
            );
        }
        catch
        {
            Release();
            throw;
        }
    }

    internal Task RemovePortAsync(nint nativePort)
    {
        lock (gate)
        {
            foreach (var row in events.Values.ToArray())
            {
                bool changed = false;
                if (row.Reader?.NativePort == nativePort)
                {
                    row.ReadGeneration++;
                    row.ReadPending = false;
                    row.Reader = null;
                    row.ReaderImage?.Dispose();
                    row.ReaderImage = null;
                    changed = true;
                }
                if (row.Writer?.NativePort == nativePort)
                {
                    row.WriteGeneration++;
                    row.WritePending = false;
                    row.Writer = null;
                    row.WriterImage?.Dispose();
                    row.WriterImage = null;
                    changed = true;
                }
                if (row.User?.NativePort == nativePort)
                {
                    row.User = null;
                    row.UserImage?.Dispose();
                    row.UserImage = null;
                    changed = true;
                }
                if (!changed) continue;
                row.Generation = checked(row.Generation + 1);
                var wait = row.Wait;
                row.Wait = null;
                if (row.Reader is { } otherReader)
                    retirement.Add(Reconfigure(
                        row,
                        wait,
                        row.Pending.ToArray(),
                        otherReader,
                        null,
                        false,
                        row.Generation
                    ));
                else if (row.Writer is { } otherWriter)
                    retirement.Add(Reconfigure(
                        row,
                        wait,
                        row.Pending.ToArray(),
                        otherWriter,
                        null,
                        false,
                        row.Generation
                    ));
                else if (wait is not null) retirement.Add(wait.CancelAsync());
                if (row.Reader is null && row.Writer is null && row.User is null && !row.StopPending) events.Remove(row.Handle);
            }

            return Task.WhenAll(retirement.ToArray());
        }
    }

    internal async Task CloseAsync()
    {
        Task[] owned;
        lock (gate)
        {
            closing = true;
            foreach (var row in events.Values)
            {
                if (!row.StopPending) row.Generation = checked(row.Generation + 1);
                if (row.Wait is { } wait) retirement.Add(wait.CancelAsync());
                row.ReaderImage?.Dispose();
                row.WriterImage?.Dispose();
                row.UserImage?.Dispose();
            }
            events.Clear();
            owned = retirement.ToArray();
        }
        await Task.WhenAll(owned).ConfigureAwait(false);
    }
}

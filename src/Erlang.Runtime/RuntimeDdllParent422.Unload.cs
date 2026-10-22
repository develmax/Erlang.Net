namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal async ValueTask<Term> TryUnload(ProcessContext context, Term nameValue, Term optionsValue)
    {
        runtime.RequireDriverParentActor422(context);
        ErlDriverUnloadOptions420 options = ErlDriverUnloadOptions420.Parse(optionsValue, context.Cancellation);
        ErlDriverName420 name = ErlDriverName420.Parse(nameValue, context.Cancellation);
        while (true)
        {
            Task? pending;
            DdllParentRow422? kill = null;
            Term? result = null;
            lock (gate)
            {
                runtime.RequireDriverParentActor422(context);
                ObjectDisposedException.ThrowIf(closing, this);
                pending = mutation?.Task;
                if (pending is null)
                {
                    DdllParentRow422? row = Find(name);
                    if (row is null) return SoftError(DdllParentAtoms422.NotLoaded);
                    if (row.Linked) return SoftError(DdllParentAtoms422.Linked);
                    if (row.State == DdllParentStateKind422.Permanent) return SoftError(DdllParentAtoms422.Permanent);
                    var mine = row.Processes.First;
                    while (mine is not null && !(mine.Value.Kind == DdllParentReferenceKind422.Loaded && mine.Value.Owner.Equals(context.Self))) mine = mine.Next;
                    if (mine is null && row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded))
                        return SoftError(DdllParentAtoms422.NotLoadedByCaller);
                    if (mine is not null) row.Processes.Remove(mine);
                    bool otherUsers = row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded);
                    if (otherUsers)
                    {
                        return Ok(
                            DdllParentAtoms422.PendingProcess,
                            options.Monitor == ErlDriverWait420.Pending
                            ? AddMonitor(row, context.Self, DdllParentReferenceKind422.AwaitingUnload) : null
                        );
                    }
                    if (row.State is DdllParentStateKind422.Reloading or DdllParentStateKind422.ForceReloading)
                    {
                        NotifyAll(
                            row,
                            kind => kind == DdllParentReferenceKind422.AwaitingLoad,
                            DdllParentAtoms422.Down,
                            Term.A(DdllParentAtoms422.LoadCancelled)
                        );
                        row.ReloadPath = null;
                    }
                    bool force = options.KillPorts || row.KillPorts;
                    row.State = force && row.Ports.Count != 0 ? DdllParentStateKind422.ForceUnloading : DdllParentStateKind422.Unloading;
                    result = Ok(
                        DdllParentAtoms422.PendingDriver,
                        options.Monitor == ErlDriverWait420.None
                        ? null : AddMonitor(row, context.Self, DdllParentReferenceKind422.AwaitingUnload)
                    );
                    if (force && row.Ports.Count != 0) kill = row;
                    QueueRetirement(row);
                }
            }
            if (pending is not null)
            {
                await pending.WaitAsync(context.Cancellation).ConfigureAwait(false);
                continue;
            }
            if (kill is not null) await killPorts(kill).ConfigureAwait(false);

            return result ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }
    }

    internal async Task ProcessTerminated(Pid caller)
    {
        while (true)
        {
            Task? pending;
            var killed = new List<DdllParentRow422>();
            lock (gate)
            {
                pending = mutation?.Task;
                if (pending is null)
                {
                    foreach (var row in drivers)
                    {
                        if (row.Linked || row.State == DdllParentStateKind422.Permanent) continue;
                        for (var node = row.Processes.First; node is not null;)
                        {
                            var next = node.Next;
                            if (node.Value.Owner.Equals(caller)) row.Processes.Remove(node);
                            node = next;
                        }
                        if (row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded)) continue;
                        if (row.State is DdllParentStateKind422.Reloading or DdllParentStateKind422.ForceReloading)
                            NotifyAll(
                                row,
                                kind => kind == DdllParentReferenceKind422.AwaitingLoad,
                                DdllParentAtoms422.Down,
                                Term.A(DdllParentAtoms422.LoadCancelled)
                            );
                        row.ReloadPath = null;
                        row.State = row.KillPorts && row.Ports.Count != 0 ? DdllParentStateKind422.ForceUnloading : DdllParentStateKind422.Unloading;
                        if (row.KillPorts && row.Ports.Count != 0) killed.Add(row);
                        QueueRetirement(row);
                    }
                }
            }
            if (pending is not null)
            {
                await pending.ConfigureAwait(false);
                continue;
            }
            var failures = new List<Exception>();
            foreach (var row in killed)
                try
                {
                    await killPorts(row).ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (failures.Count != 0) throw new AggregateException(failures);

            return;
        }
    }

    // Called only after final child PortStopped admission, not logical failure.
    internal void PortStopped(ulong driver, ulong port)
    {
        lock (gate)
        {
            if (!identities.TryGetValue(driver, out var row) || !row.Ports.Remove(port))
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            QueueRetirement(row);
        }
    }

    internal DdllParentRow422 AcquirePortCore(ErlDriverName420 name, ulong port)
    {
        // Native port open invokes this under the shared runtime gate after the
        // stable-mutation wait and before child start; reference covers rollback.
        ObjectDisposedException.ThrowIf(closing, this);
        if (mutation is not null) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
        var row = Find(name) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (row.State is DdllParentStateKind422.ForceUnloading or DdllParentStateKind422.ForceReloading)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!row.Ports.Add(port)) throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return row;
    }
}

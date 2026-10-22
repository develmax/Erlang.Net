namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    private Task? shutdown;

    internal Task BeginClose()
    {
        lock (gate)
        {
            if (shutdown is not null) return shutdown;
            closing = true;
            bool suppress = !ExecutionContext.IsFlowSuppressed();
            AsyncFlowControl flow = default;
            try
            {
                if (suppress) flow = ExecutionContext.SuppressFlow();

                return shutdown = CloseCore();
            }
            finally
            {
                if (suppress) flow.Undo();
            }
        }
    }

    private async Task CloseCore()
    {
        Task? pending;
        while (true)
        {
            DdllParentRow422[]? rows = null;
            lock (gate)
            {
                pending = mutation?.Task;
                if (pending is null)
                {
                    rows = drivers.Where(row => !row.Linked).ToArray();
                    foreach (var row in rows)
                    {
                        row.Processes.Clear();
                        row.ReloadPath = null;
                        row.State = DdllParentStateKind422.ForceUnloading;
                        QueueRetirement(row);
                    }
                }
            }
            if (pending is not null)
            {
                try
                {
                    await pending.ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    var pendingFailures = new List<Exception> { failure };
                    try
                    {
                        await worker.ConfigureAwait(false);
                    }
                    catch (Exception workerFailure)
                    {
                        pendingFailures.Add(workerFailure);
                    }
                    throw new AggregateException(pendingFailures);
                }
                continue;
            }
            var failures = new List<Exception>();
            foreach (var row in rows!)
                try
                {
                    await killPorts(row).ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            wake.Release();
            try
            {
                await worker.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (failures.Count != 0) throw new AggregateException(failures);

            return;
        }
    }

    private async Task RetireLoop()
    {
        while (true)
        {
            await wake.WaitAsync().ConfigureAwait(false);
            while (true)
            {
                Task? pending;
                DdllParentRow422? selected = null;
                TaskCompletionSource? transaction = null;
                lock (gate)
                {
                    pending = mutation?.Task;
                    if (pending is null)
                    {
                        while (retiring.TryDequeue(out var row))
                        {
                            row.RetirementQueued = false;
                            if (!identities.TryGetValue(row.Identity, out var actual) || !ReferenceEquals(actual, row)
                                || row.Ports.Count != 0 || row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded && !entry.Deferred)) continue;
                            if (!closing && row.State is not (DdllParentStateKind422.Unloading or DdllParentStateKind422.ForceUnloading
                                or DdllParentStateKind422.Reloading or DdllParentStateKind422.ForceReloading)) continue;
                            selected = row;
                            transaction = BeginMutation();
                            row.Node.List?.Remove(row.Node); // source removes before native finish.
                            break;
                        }
                        if (selected is null && closing && !identities.Values.Any(row => !row.Linked)) return;
                    }
                }
                if (pending is not null)
                {
                    await pending.ConfigureAwait(false);
                    continue;
                }
                if (selected is null) break;
                try
                {
                    await RetireOne(selected).ConfigureAwait(false);
                    lock (gate) EndMutation(transaction!);
                }
                catch (Exception failure)
                {
                    lock (gate)
                    {
                        // An unknown native owner outcome is a host failure. Do
                        // not reopen guest admission or invent an unload result.
                        closing = true;
                        transaction!.TrySetException(failure);
                    }
                    throw;
                }
                selected = null;
            }
        }
    }

    private async Task RetireOne(DdllParentRow422 row)
    {
        ulong oldIdentity = row.Identity;
        await transport.Unload(oldIdentity).ConfigureAwait(false);
        ErlDriverPath420? reload;
        bool flags;
        lock (gate)
        {
            identities.Remove(oldIdentity);
            NotifyAll(
                row,
                kind => kind is DdllParentReferenceKind422.AwaitingUnload or DdllParentReferenceKind422.AwaitingUnloadOnly,
                DdllParentAtoms422.Down,
                Term.A(DdllParentAtoms422.Unloaded)
            );
            reload = closing ? null : row.ReloadPath;
            flags = row.ReloadKillPorts;
            if (reload is null)
            {
                row.Processes.Clear();

                return;
            }
            ulong next = checked(nextIdentity + 1);
            row.Identity = next;
            row.Path = reload;
            row.State = DdllParentStateKind422.Loading;
            identities.Add(next, row);
            try
            {
                drivers.AddFirst(row.Node);
            }
            catch
            {
                identities.Remove(next);
                throw;
            }
            nextIdentity = next;
        }
        var loaded = await transport.Load(
            row.Identity,
            row.Name,
            reload,
            CancellationToken.None
        ).ConfigureAwait(false);
        int error = loaded.Error;
        lock (gate)
        {
            row.ReloadPath = null;
            if (error == 0)
            {
                row.NativeFlags = loaded.Flags;
                row.KillPorts = flags;
                row.State = DdllParentStateKind422.Healthy;
                for (var node = row.Processes.First; node is not null; node = node.Next)
                    if (node.Value.Kind == DdllParentReferenceKind422.Loaded) node.Value = node.Value with { Deferred = false };
                NotifyAll(
                    row,
                    kind => kind == DdllParentReferenceKind422.AwaitingLoad,
                    DdllParentAtoms422.Up,
                    Term.A(DdllParentAtoms422.Loaded)
                );
                if (!row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded))
                {
                    row.State = DdllParentStateKind422.Unloading;
                    QueueRetirement(row);
                }
            }
            else
            {
                row.Node.List?.Remove(row.Node);
                identities.Remove(row.Identity);
                NotifyAll(
                    row,
                    kind => kind == DdllParentReferenceKind422.AwaitingLoad,
                    DdllParentAtoms422.Down,
                    Term.Tuple(Term.A(DdllParentAtoms422.LoadFailure), DdllParentLoadError422.Value(error))
                );
                row.Processes.Clear();
            }
        }
    }
}

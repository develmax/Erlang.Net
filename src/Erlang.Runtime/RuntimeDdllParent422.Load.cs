namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal async ValueTask<Term> TryLoad(
        ProcessContext context,
        Term pathValue,
        Term nameValue,
        Term optionsValue
    )
    {
        runtime.RequireDriverParentActor422(context);
        ErlDriverLoadOptions420 options = ErlDriverLoadOptions420.Parse(optionsValue, context.Cancellation);
        ErlDriverName420 name = ErlDriverName420.Parse(nameValue, context.Cancellation);
        ErlDriverPath420 path = ErlDriverPath420.Parse(pathValue, name, context.Cancellation);
        while (true)
        {
            Task? pending;
            TaskCompletionSource? transaction = null;
            DdllParentRow422? created = null, kill = null;
            Term? result = null;
            lock (gate)
            {
                runtime.RequireDriverParentActor422(context);
                ObjectDisposedException.ThrowIf(closing, this);
                pending = PendingMutation438;
                if (pending is null)
                {
                    DdllParentRow422? row = Find(name);
                    if (row is null)
                    {
                        if (options.Reload != ErlDriverWait420.None) return SoftError(DdllParentAtoms422.NotLoaded);
                        ulong identity = checked(nextIdentity + 1);
                        created = new(
                            identity,
                            name,
                            path,
                            false,
                            identity,
                            options.KillPorts
                        );
                        transaction = BeginMutation();
                        try
                        {
                            identities.Add(identity, created);
                            drivers.AddFirst(created.Node);
                        }
                        catch
                        {
                            identities.Remove(identity);
                            EndMutation(transaction);
                            throw;
                        }
                        nextIdentity = identity;
                    }
                    else if (row.Linked) return SoftError(DdllParentAtoms422.Linked);
                    else if (row.State == DdllParentStateKind422.Permanent) return SoftError(DdllParentAtoms422.Permanent);
                    else if (row.State == DdllParentStateKind422.Healthy)
                    {
                        if (options.Reload == ErlDriverWait420.PendingDriver
                            && !(row.Processes.Count(entry => entry.Kind == DdllParentReferenceKind422.Loaded) == 1
                                && row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded && entry.Owner.Equals(context.Self))))
                            return SoftError(DdllParentAtoms422.PendingProcess);
                        if (options.Reload != ErlDriverWait420.None)
                        {
                            if (row.KillPorts != options.KillPorts) return SoftError(DdllParentAtoms422.Inconsistent);
                            var mine = row.Processes.First;
                            while (mine is not null && !(mine.Value.Kind == DdllParentReferenceKind422.Loaded && mine.Value.Owner.Equals(context.Self))) mine = mine.Next;
                            if (mine is null) return SoftError(DdllParentAtoms422.NotLoadedByCaller);
                            row.Processes.Remove(mine);
                            for (var node = row.Processes.First; node is not null; node = node.Next)
                                if (node.Value.Kind == DdllParentReferenceKind422.Loaded) node.Value = node.Value with { Deferred = true };
                            row.Processes.AddFirst(new DdllParentProcessReference422(context.Self, DdllParentReferenceKind422.Loaded, Deferred: true));
                            row.ReloadPath = path;
                            row.ReloadKillPorts = options.KillPorts;
                            row.State = row.KillPorts ? DdllParentStateKind422.ForceReloading : DdllParentStateKind422.Reloading;
                            result = Ok(
                                options.Reload == ErlDriverWait420.PendingDriver ? DdllParentAtoms422.PendingDriver : DdllParentAtoms422.PendingProcess,
                                options.Monitor == ErlDriverWait420.None ? null : AddMonitor(row, context.Self, DdllParentReferenceKind422.AwaitingLoad)
                            );
                            if (row.KillPorts && row.Ports.Count != 0) kill = row;
                            QueueRetirement(row);
                        }
                        else
                        {
                            if (row.Path!.Key != path.Key || row.KillPorts != options.KillPorts) return SoftError(DdllParentAtoms422.Inconsistent);
                            row.Processes.AddFirst(new DdllParentProcessReference422(context.Self, DdllParentReferenceKind422.Loaded));

                            return Ok(DdllParentAtoms422.AlreadyLoaded);
                        }
                    }
                    else if (row.State is DdllParentStateKind422.Unloading or DdllParentStateKind422.ForceUnloading)
                    {
                        if (options.Reload != ErlDriverWait420.None) return SoftError(DdllParentAtoms422.NotLoadedByCaller);
                        if (row.Path!.Key != path.Key || row.KillPorts != options.KillPorts) return SoftError(DdllParentAtoms422.Inconsistent);
                        row.State = DdllParentStateKind422.Healthy;
                        NotifyAll(
                            row,
                            kind => kind == DdllParentReferenceKind422.AwaitingUnload,
                            DdllParentAtoms422.Up,
                            Term.A(DdllParentAtoms422.UnloadCancelled)
                        );
                        row.Processes.AddFirst(new DdllParentProcessReference422(context.Self, DdllParentReferenceKind422.Loaded));

                        return Ok(DdllParentAtoms422.AlreadyLoaded);
                    }
                    else if (row.State is DdllParentStateKind422.Reloading or DdllParentStateKind422.ForceReloading)
                    {
                        if (options.Reload != ErlDriverWait420.None) return SoftError(DdllParentAtoms422.PendingReload);
                        if (row.ReloadPath!.Key != path.Key || row.ReloadKillPorts != options.KillPorts) return SoftError(DdllParentAtoms422.Inconsistent);
                        row.Processes.AddFirst(new DdllParentProcessReference422(context.Self, DdllParentReferenceKind422.Loaded, Deferred: true));

                        return Ok(DdllParentAtoms422.PendingDriver, AddMonitor(row, context.Self, DdllParentReferenceKind422.AwaitingLoad));
                    }
                    else throw new InvalidDataException(DriverParentWireNames422.Invalid);
                }
            }
            if (pending is not null)
            {
                await pending.WaitAsync(context.Cancellation).ConfigureAwait(false);
                continue;
            }
            if (created is not null)
            {
                DriverParentLoadResult422 loaded;
                try
                {
                    loaded = await transport.Load(
                        created.Identity,
                        name,
                        path,
                        context.Cancellation
                    ).ConfigureAwait(false);
                }
                catch
                {
                    lock (gate)
                    {
                        created.Node.List?.Remove(created.Node);
                        identities.Remove(created.Identity);
                        EndMutation(transaction!);
                    }
                    throw;
                }
                lock (gate)
                {
                    int error = loaded.Error;
                    if (error == 0)
                    {
                        created.NativeFlags = loaded.Flags;
                        created.State = DdllParentStateKind422.Healthy;
                        if (!closing && runtime.DriverTargetAlive422(context.Self))
                            created.Processes.AddFirst(new DdllParentProcessReference422(context.Self, DdllParentReferenceKind422.Loaded));
                        else
                        {
                            created.State = DdllParentStateKind422.Unloading;
                            QueueRetirement(created);
                        }
                    }
                    else
                    {
                        created.Node.List?.Remove(created.Node);
                        identities.Remove(created.Identity);
                    }
                    EndMutation(transaction!);
                }
                context.Cancellation.ThrowIfCancellationRequested();

                return loaded.Error == 0 ? Ok(DdllParentAtoms422.Loaded)
                    : Term.Tuple(Term.A(DdllParentAtoms422.Error), DdllParentLoadError422.Value(loaded.Error));
            }
            if (kill is not null) await killPorts(kill).ConfigureAwait(false);

            return result ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }
    }
}

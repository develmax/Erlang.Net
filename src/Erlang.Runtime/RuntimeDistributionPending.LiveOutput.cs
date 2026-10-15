namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private DistributionPublicationTarget CaptureLivePublicationTarget(Term handle, ProcessContext producer)
    {
        lock (gate)
        {
            RequireProcess(producer);
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            try
            {
                return new(
                    entry,
                    RequireConnected(entry, connectionId),
                    connectionId,
                    resource
                );
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
    }

    private DistributionPublicationTarget CaptureMonitorPublicationTarget(Term handle, ProcessContext observer, Term remote)
    {
        var target = CaptureLivePublicationTarget(handle, observer);
        if (remote is not Atom && (remote is not Pid pid || pid.LocalIdentity is not null ||
            pid.Node != target.Entry.Node.Name || runtime.IsCurrentLocal(pid)))
        {
            target.Dispose();
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return target;
    }

    private DistributionPublicationTarget CaptureUnlinkPublicationTarget(Term handle, ProcessContext local, Pid remote)
    {
        var target = CaptureLivePublicationTarget(handle, local);
        if (remote.LocalIdentity is not null || remote.Node != target.Entry.Node.Name || runtime.IsCurrentLocal(remote))
        {
            target.Dispose();
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return target;
    }

    public async ValueTask<ReferenceTerm> BeginConnectedMonitorSignalAsync(Term handle, ProcessContext observer, Term remote)
    {
        using var target = CaptureMonitorPublicationTarget(handle, observer, remote);

        return await runtime.BeginConnectedProcessMonitorAsync(
            target.Entry,
            target.Connection,
            observer,
            remote
        ).ConfigureAwait(false);
    }

    public async ValueTask<bool> EndConnectedMonitorSignalAsync(ProcessContext observer, ReferenceTerm reference)
    {
        ValidateMonitorRemoval(observer, reference);

        return await runtime.EndConnectedProcessMonitorAsync(observer, reference).ConfigureAwait(false);
    }

    private void ValidateMonitorRemoval(ProcessContext observer, ReferenceTerm reference)
    {
        lock (gate)
        {
            RequireProcess(observer);
            if (!runtime.IsCurrentLocal(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    public async ValueTask<ulong?> BeginConnectedUnlinkSignalAsync(Term handle, ProcessContext local, Pid remote)
    {
        using var target = CaptureUnlinkPublicationTarget(handle, local, remote);

        return await runtime.BeginConnectedProcessUnlinkAsync(
            target.Entry,
            target.Connection,
            local,
            remote
        ).ConfigureAwait(false);
    }
}

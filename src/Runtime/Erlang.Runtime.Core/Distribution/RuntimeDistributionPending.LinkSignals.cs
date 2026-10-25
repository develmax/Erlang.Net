namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    public void ApplyConnectedLinkSignal(
        Term handle,
        ProcessContext inputHandler,
        Pid remote,
        Pid local
    )
    {
        ApplyConnectedProcessSignal(
            handle,
            inputHandler,
            remote,
            local,
            (entry, connected) =>
            runtime.QueueConnectedProcessLink(
                entry,
                connected,
                remote,
                local
            )
        );
    }

    public void ApplyConnectedUnlinkSignal(
        Term handle,
        ProcessContext inputHandler,
        Pid remote,
        Pid local,
        ulong unlinkId,
        bool acknowledgement
    )
    {
        if (!acknowledgement && unlinkId == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ApplyConnectedProcessSignal(
            handle,
            inputHandler,
            remote,
            local,
            (entry, connected) =>
        {
            if (!runtime.IsCurrentLocal(local))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            runtime.QueueConnectedProcessUnlink(
                entry,
                connected,
                remote,
                local,
                unlinkId,
                acknowledgement
            );
        }
        );
    }

    public void ApplyConnectedExitSignal(
        Term handle,
        ProcessContext inputHandler,
        Pid remote,
        Pid local,
        Term reason,
        bool explicitSignal
    )
    {
        ArgumentNullException.ThrowIfNull(reason);
        ApplyConnectedProcessSignal(
            handle,
            inputHandler,
            remote,
            local,
            (entry, connected) =>
        {
            if (!explicitSignal && !runtime.IsCurrentLocal(local))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            runtime.QueueConnectedProcessExit(
                entry,
                connected,
                remote,
                local,
                reason,
                explicitSignal
            );
        }
        );
    }

    public ulong? BeginConnectedUnlinkSignal(Term handle, ProcessContext local, Pid remote)
    {
        using var target = CaptureUnlinkPublicationTarget(handle, local, remote);

        return runtime.BeginConnectedProcessUnlink(
            target.Entry,
            target.Connection,
            local,
            remote
        );
    }

    private void ApplyConnectedProcessSignal(
        Term handle,
        ProcessContext inputHandler,
        Pid remote,
        Pid local,
        Action<RuntimeDistributionPendingEntry, DistributionConnectedEntry> signal
    )
    {
        using var outputTransfer = BeginOutputTransfer();
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(local);
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);
                RequireInputHandler(connected, inputHandler);
                if (remote.LocalIdentity is not null || remote.Node != entry.Node.Name || runtime.IsCurrentLocal(remote) || local.Node != runtime.Node ||
                    local.LocalIdentity is not null && !ReferenceEquals(local.LocalIdentity, runtime.LocalIdentity))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                signal(entry, connected);
            }
        }
    }

    internal bool IsConnectedSignalGeneration(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId
    ) => entry.ConnectionId == connectionId && entry.State == DistributionPendingState.Connected &&
        ReferenceEquals(entry.Connected, connected) && (connected.NativePort457 is { } port
            ? port.IsLive : connected.Controller is { } controller && runtime.IsOwnedLiveNodeProcess(controller));


}

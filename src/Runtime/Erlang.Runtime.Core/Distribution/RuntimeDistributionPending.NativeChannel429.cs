namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly HashSet<NativeDistChannelRequest429> nativeChannelRequests429 = new();

    internal Term CreateNativePidChannel429(
        ProcessContext caller,
        Atom node,
        ProcessContext controller,
        ProcessContext kernel,
        ulong flags,
        uint creation
    )
    {
        lock (gate)
        {
            if (closed || !pending.TryGetValue(node.Name, out var entry)) return Term.A(DistChannel429Names.BadArgument);
            if (entry.State == DistributionPendingState.Exiting) throw new NotSupportedException(DistChannel429Names.DownFrontier);
            if (entry.State != DistributionPendingState.Pending || entry.Connected is not null
             || entry.ReservedController is not null || controller.DistributionRole != RuntimeDistributionRole.None
             || exitingInputControllers.Contains(controller) || ReferenceEquals(controller, kernel)) return Term.A(DistChannel429Names.BadArgument);
            ResourceReferenceTerm handle = runtime.ReferenceResources.CreateConnectedDistributionHandle(entry);
            try
            {
                var request = new NativeDistChannelRequest429(
                    entry,
                    controller,
                    kernel,
                    caller.Self,
                    runtime.MakeReference(),
                    handle,
                    flags,
                    creation
                );
                if (ReferenceEquals(controller, caller))
                {
                    try
                    {
                        Term result = CompleteNativeChannel429(request);

                        return result is Atom ? result : Term.Tuple(Term.A(DistChannel429Names.Ok), result);
                    }
                    catch
                    {
                        handle.Entry.Close();
                        throw;
                    }
                }
                nativeChannelRequests429.Add(request);
                try
                {
                    controller.Mailbox.SendPending(
                        Nil.Value,
                        () =>
                    {
                        lock (gate)
                        {
                            if (nativeChannelRequests429.Remove(request)) ReplyNativeChannel429(request, CompleteNativeChannel429(request));
                        }

                        return null;
                    }
                    );

                    return Term.Tuple(Term.A(DistChannel429Names.Message), request.Reply);
                }
                catch
                {
                    nativeChannelRequests429.Remove(request);
                    handle.Entry.Close();
                    throw;
                }
            }
            catch
            {
                handle.Entry.Close();
                throw;
            }
        }
    }

    private Term CompleteNativeChannel429(NativeDistChannelRequest429 request)
    {
        // Revalidate the actual captured generation, controller and kernel. No host
        // wrapper/error-stack projection is used for the native badarg result.
        if (closed || request.Entry.ConnectionId != request.Generation || request.Entry.State != DistributionPendingState.Pending
         || request.Entry.Connected is not null || request.Entry.ReservedController is not null
         || request.Controller.DistributionRole != RuntimeDistributionRole.None || exitingInputControllers.Contains(request.Controller)
         || !ReferenceEquals(runtime.ReadNativeDistributionKernel429(false), request.Kernel)
         || !ReferenceEquals(runtime.ReadNativeDistributionController429(request.Controller.Self), request.Controller))
        {
            request.Handle.Entry.Close();

            return Term.A(DistChannel429Names.BadArgument);
        }
        try
        {
            AdoptConnectedOwned(
                Term.Tuple(Term.I(request.Generation), request.Entry.Reference),
                request.Controller,
                request.Flags,
                request.Creation,
                request.Kernel,
                request.Setup
            );

            return Term.Tuple(Term.I(request.Generation), request.Handle);
        }
        catch
        {
            request.Handle.Entry.Close();
            throw;
        }
    }

    private void ReplyNativeChannel429(NativeDistChannelRequest429 request, Term value)
     => runtime.SendSystem(request.Setup, Term.Tuple(request.Reply, value), Term.A(DistChannel429Names.System));

    private void RetireNativeChannelRequests429(RuntimeDistributionPendingEntry? entry, ProcessContext? controller)
    {
        foreach (var request in nativeChannelRequests429.Where(request => ReferenceEquals(request.Entry, entry) || ReferenceEquals(request.Controller, controller)).ToArray())
        {
            nativeChannelRequests429.Remove(request);
            request.Handle.Entry.Close();
            ReplyNativeChannel429(request, Term.A(DistChannel429Names.BadArgument));
        }
    }

    internal static bool IsNativeConnectedHandle429(Term value)
     => value is TupleTerm { Items.Count: 2 } t && t.Items[1] is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.ConnectedDistributionHandle };

    internal TupleTerm ResolveNativeConnectedHandle429(Term value)
    {
        lock (gate)
        {
            if (value is not TupleTerm { Items.Count: 2 } envelope || envelope.Items[1] is not ReferenceTerm reference)
                throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(value));
            using var lease = runtime.ReferenceResources.AcquireConnectedDistributionHandle(reference)
             ?? throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(value));
            DistributionConnectedHandle handle = lease.Value;
            if (!ReferenceEquals(handle.Entry.Owner, this) || !envelope.Items[0].Equals(Term.I(handle.ConnectionId))) throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(value));
            RequireOpen();
            if (handle.Entry.ConnectionId != handle.ConnectionId || handle.Entry.State != DistributionPendingState.Connected
             || handle.Entry.Connected is not { } connected || (connected.NativePort457 is { } port ? !port.IsLive : connected.Controller?.DistributionRole != RuntimeDistributionRole.Controller)
             || runtime.ReadNativeDistributionKernel429(false) is null
             || connected.Controller is { } controller && !ReferenceEquals(runtime.ReadNativeDistributionController429(controller.Self), controller))
                throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(value));

            return Term.Tuple(Term.I(handle.ConnectionId), handle.Entry.Reference);
        }
    }
}

namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly HashSet<DistributionControllerAdoptionRequest> controllerAdoptions = new();

    public async ValueTask<Term> AdoptNodeControllerAsync(
        ProcessContext requester,
        Term node,
        Term controller,
        Term options
    )
    {
        if (node is not Atom remoteNode || !DistributionNodeIdentity.IsValid(remoteNode) ||
            controller is not Pid pid || options is not TupleTerm { Items.Count: 2 } tuple ||
            tuple.Items[0] is not Integer flags || flags.Value < ulong.MinValue || flags.Value > ulong.MaxValue ||
            !RuntimeNodeCreation.TryConvert(tuple.Items[1], out uint creation))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        DistributionControllerAdoptionRequest request;
        lock (gate)
        {
            RequireProcess(requester);
            var kernel = runtime.RequireDistributionKernel(requireRegistered: true);
            var owner = runtime.RequireDistributionControllerTarget(pid);
            if (remoteNode.Name == runtime.Node || !pending.TryGetValue(remoteNode.Name, out var entry) ||
                entry.State != DistributionPendingState.Pending ||
                !DistributionCapabilityProfile429.HasMandatory((ulong)flags.Value))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (DistributionCapabilityProfile429.HasUnsupported((ulong)flags.Value))
                throw new ErlangException(DistributionControllerNames.NotSupported);
            request = new(
                entry,
                owner,
                kernel,
                requester.Self,
                runtime.MakeReference(),
                (ulong)flags.Value,
                creation
            );
            if (ReferenceEquals(owner, requester))
            {
                Term immediate = CompleteControllerAdoption(request);
                if (immediate is Atom immediateReason)
                    throw new ErlangException(immediateReason.Name);

                return immediate;
            }
            controllerAdoptions.Add(request);
            owner.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (controllerAdoptions.Remove(request))
                        ReplyControllerAdoption(request, CompleteControllerAdoption(request));
                }

                return null;
            }
            );
        }
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } result &&
            result.Items[0].Equals(request.ReplyId) ? result : null);
        Term value = reply!.Items[1];
        if (value is Atom reason)
            throw new ErlangException(reason.Name);

        return value;
    }

    internal TupleTerm ResolveConnectedControllerHandle(Term handle)
    {
        lock (gate)
        {
            if (IsNativeConnectedHandle429(handle)) return ResolveNativeConnectedHandle429(handle);
            var entry = ReadHandle(handle, out uint connectionId, out var lease);
            using (lease)
            {
                RequireConnected(entry, connectionId);

                return Term.Tuple(Term.I(connectionId), entry.Reference);
            }
        }
    }

    internal bool HasConnectedControllerHandle(Term handle)
    {
        lock (gate)
        {
            try
            {
                if (IsNativeConnectedHandle429(handle))
                {
                    _ = ResolveNativeConnectedHandle429(handle);

                    return true;
                }
                _ = ReadHandle(handle, out _, out var lease);
                lease.Dispose();

                return true;
            }
            catch (Exception failure) when (failure is ArgumentException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    private Term CompleteControllerAdoption(DistributionControllerAdoptionRequest request)
    {
        try
        {
            ValidateEntry(request.Entry, request.ConnectionId);
            TupleTerm handle = Term.Tuple(Term.I(request.ConnectionId), request.Entry.Reference);
            AdoptConnectedOwned(
                handle,
                request.Controller,
                request.Flags,
                request.Creation,
                request.Kernel,
                request.SetupPid
            );

            return handle;
        }
        catch (ErlangException exception)
        {
            return exception.Reason;
        }
        catch (NotSupportedException)
        {
            return Term.A(DistributionControllerNames.NotSupported);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Term.A(ErlangErrorReasons.BadArgument);
        }
    }

    private void ReplyControllerAdoption(DistributionControllerAdoptionRequest request, Term value)
        => runtime.SendSystem(request.SetupPid, Term.Tuple(request.ReplyId, value), request.Controller.Self);

    private void RetireControllerAdoptions(RuntimeDistributionPendingEntry? entry = null, ProcessContext? controller = null)
    {
        RetireNativeChannelRequests429(entry, controller);
        foreach (var request in controllerAdoptions.Where(request =>
            ReferenceEquals(request.Entry, entry) || ReferenceEquals(request.Controller, controller)).ToArray())
        {
            controllerAdoptions.Remove(request);
            ReplyControllerAdoption(request, Term.A(ErlangErrorReasons.BadArgument));
        }
    }
}

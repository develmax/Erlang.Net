namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly HashSet<ProcessContext> exitingInputControllers = new();

    internal Term PutConnectedData(ProcessContext caller, Term handle, Term data)
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        uint connectionId;
        ReferenceResourceLease<DistributionConnectedHandle> resource;
        BitString binary;
        lock (gate)
        {
            RequireProcess(caller);
            if (handle is not ReferenceTerm reference)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            resource = runtime.ReferenceResources.AcquireConnectedDistributionHandle(reference)
                ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
            try
            {
                var captured = resource.Value;
                entry = captured.Entry;
                connectionId = captured.ConnectionId;
                if (!ReferenceEquals(entry.Owner, this))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (entry.Connected is not { } active || active.InputHandler is not { } inputHandler ||
                    !inputHandler.Equals(caller.Self))
                    throw new ErlangException(DistributionControllerNames.NotSupported);
                connected = active;
                connected.RawInputAttempts = unchecked(connected.RawInputAttempts + 1);
                if (data is Nil)
                {
                    resource.Dispose();

                    return Term.A(CommonAtomNames.Ok);
                }
                if (data is not BitString bits || !bits.IsBinary)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                binary = bits;
                if (binary.ByteLength == 0 || entry.ConnectionId != connectionId ||
                    entry.State != DistributionPendingState.Connected)
                {
                    resource.Dispose();

                    return Term.A(CommonAtomNames.Ok);
                }
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
        using (resource)
        {
            try
            {
                AcceptConnectedInput(
                    Term.Tuple(Term.I(connectionId), entry.Reference),
                    caller,
                    binary.ByteSpan,
                    true
                );
            }
            catch (InvalidOperationException)
            {
                // Controller/input/snapshot revalidation races are not malformed packets.
            }
            catch (Exception exception) when (exception is ArgumentException or
                EndOfStreamException or InvalidDataException or FormatException or OverflowException ||
                exception is ErlangException error && error.Reason is Atom { Name: ErlangErrorReasons.BadArgument or ErlangErrorReasons.SystemLimit })
            {
                lock (gate)
                {
                    if (entry.ConnectionId == connectionId && ReferenceEquals(entry.Connected, connected) &&
                        entry.State == DistributionPendingState.Connected)
                    {
                        entry.State = DistributionPendingState.Exiting;
                        exitingInputControllers.Add(connected.Controller);
                        runtime.QueueDistributionControllerKill(connected.Controller);
                    }
                }
            }

            return Term.A(CommonAtomNames.Ok);
        }
    }
}

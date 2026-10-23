namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly HashSet<ProcessContext> exitingInputControllers = new();

    internal Term PutConnectedData(ProcessContext caller, Term handle, Term data)
    {
        using var outputTransfer = BeginOutputTransfer();
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        uint connectionId;
        ReferenceResourceLease<RuntimeDistributionPendingEntry> resource;
        BitString binary;
        lock (gate)
        {
            RequireProcess(caller);
            try
            {
                if (IsNativeConnectedHandle429(handle)) handle = ResolveNativeConnectedHandle429(handle);
                entry = ReadHandle(handle, out connectionId, out resource);
            }
            catch (ArgumentException)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            try
            {
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
                        if (connected.Controller is { } controller)
                        {
                            exitingInputControllers.Add(controller);
                            runtime.QueueDistributionControllerKill(controller);
                        }
                        else RetirePending(entry);
                    }
                }
            }

            return Term.A(CommonAtomNames.Ok);
        }
    }
}

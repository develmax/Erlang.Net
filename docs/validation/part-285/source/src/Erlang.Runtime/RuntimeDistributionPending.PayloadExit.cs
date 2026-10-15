namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void RouteDeferredConnectedExit(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        DistributionPayloadExitEnvelope envelope
    )
    {
        var items = envelope.Control.Items;
        int opcode = (int)((Integer)items[DistributionControlOpcodes.OpcodeIndex]).Value;
        var remote = (Pid)items[DistributionControlOpcodes.FromIndex];
        var local = (Pid)items[DistributionControlOpcodes.TargetIndex];
        if (remote.LocalIdentity is not null || remote.Node != entry.Node.Name || runtime.IsCurrentLocal(remote) ||
            local.Node != runtime.Node || local.LocalIdentity is not null && !ReferenceEquals(local.LocalIdentity, runtime.LocalIdentity))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool explicitSignal = opcode is DistributionControlOpcodes.PayloadExit2 or DistributionControlOpcodes.PayloadExit2Trace;
        if (!explicitSignal && !runtime.IsCurrentLocal(local))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term token = opcode is DistributionControlOpcodes.PayloadExitTrace or DistributionControlOpcodes.PayloadExit2Trace
            ? items[DistributionControlOpcodes.ExitTraceTokenIndex] : Nil.Value;
        Action<Exception> failure = CaptureDeferredReasonFailure(Term.Tuple(Term.I(entry.ConnectionId), entry.Reference));
        runtime.QueueConnectedDeferredExit(
            entry,
            connected,
            remote,
            local,
            envelope.Reason,
            token,
            explicitSignal,
            failure
        );
    }
}

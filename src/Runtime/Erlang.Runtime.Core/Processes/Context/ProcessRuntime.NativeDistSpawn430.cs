namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term FailNativeDistSpawn430(
        ProcessContext owner,
        ProcessSpawnRequestOptions options,
        string reason,
        bool monitor,
        bool request,
        RuntimeProcessPayload641? input641 = null
    )
    {
        lock (gate)
        {
            owner.Cancellation.ThrowIfCancellationRequested();
            RequireAlive(owner.Self);
            ReferenceTerm reference = reason == DistSpawn430Names.BadOptions ? MakeReference() : ReserveConnectedSpawnReference(owner, options.Spawn);
            if (options.ErrorReply) SendMonitorPayload641(
                owner,
                Term.Tuple(
                    input641 is null ? options.Tag : ((TupleTerm)input641.Value641).Items[0],
                    reference,
                    Term.A(DistSpawn430Names.Error),
                    Term.A(reason)
                ),
                null,
                input641,
                preserve: true,
                from: NodeAtom
            );

            return request ? reference : Term.Tuple(reference, Term.A(monitor ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
    }
}

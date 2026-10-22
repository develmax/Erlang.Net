namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term FailNativeDistSpawn430(
        ProcessContext owner,
        ProcessSpawnRequestOptions options,
        string reason,
        bool monitor,
        bool request
    )
    {
        lock (gate)
        {
            owner.Cancellation.ThrowIfCancellationRequested();
            RequireAlive(owner.Self);
            ReferenceTerm reference = reason == DistSpawn430Names.BadOptions ? MakeReference() : ReserveConnectedSpawnReference(owner, options.Spawn);
            if (options.ErrorReply) owner.Mailbox.SendTraced(
                Term.Tuple(
                    options.Tag,
                    reference,
                    Term.A(DistSpawn430Names.Error),
                    Term.A(reason)
                ),
                null,
                preserve: true,
                from: NodeAtom
            );

            return request ? reference : Term.Tuple(reference, Term.A(monitor ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
    }
}

namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<ConnectedSpawnOutcome?> TrySpawnConnectedAsync(
        ProcessContext owner,
        string node,
        SequenceSpawnDescription description,
        ProcessSpawnOptions options
    )
    {
        var requestOptions = ProcessSpawnRequestOptions.Parse(Cons.Items(description.Options).ToArray(), true);
        ReferenceTerm? reference = await TryStartConnectedSpawnRequestAsync(
            owner,
            node,
            description,
            requestOptions
        );
        if (reference is null)
            return null;
        var reply = await owner.ReceiveAsync(value => value is TupleTerm
        {
            Items: [Atom { Name: ProcessSpawnRequestNames.DefaultTag }, var correlation, Atom, _]
        } tuple && correlation.Equals(reference) ? tuple : null);
        if (reply!.Items[2] is Atom { Name: ProcessSpawnRequestNames.Success } && reply.Items[3] is Pid child)
            return new(options.Monitor ? Term.Tuple(child, reference) : child, null);

        return new(null, reply.Items[3]);
    }
}

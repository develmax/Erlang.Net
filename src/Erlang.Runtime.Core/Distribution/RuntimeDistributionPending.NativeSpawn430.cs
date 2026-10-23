namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<Term> StartNativeDistSpawn430(
        ProcessContext caller,
        Atom node,
        SequenceSpawnDescription description,
        ProcessSpawnRequestOptions options,
        bool request
    )
    {
        ValueTask<ReferenceTerm?> operation;
        lock (gate)
        {
            RequireOpen();
            caller.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(caller);
            if (!runtime.IsDistributionAlive) return runtime.FailNativeDistSpawn430(
                caller,
                options,
                DistSpawn430Names.NoConnection,
                options.Spawn.Monitor,
                request
            );
            if (!entries.TryGetValue(node.Name, out var weak) || !weak.TryGetTarget(out var entry)) throw new NotSupportedException(DistSpawn430Names.Pending);
            if (entry.State == DistributionPendingState.Exiting) return runtime.FailNativeDistSpawn430(
                caller,
                options,
                DistSpawn430Names.NoConnection,
                options.Spawn.Monitor,
                request
            );
            if (entry.Connected is not { } connected || !IsConnectedSignalGeneration(entry, connected, entry.ConnectionId)) throw new NotSupportedException(DistSpawn430Names.Pending);
            if ((connected.Flags & DistributionPassThroughFlags.Spawn) == 0) return runtime.FailNativeDistSpawn430(
                caller,
                options,
                DistSpawn430Names.NotSupported,
                options.Spawn.Monitor,
                request
            );
            // Existing owner reserves the actual request/reference/output ticket synchronously
            // under this same gate; payload encoding and physical publication await outside.
            operation = caller.AsyncDistribution
             ? StartNativeDistSpawnAsynchronously430(
                 caller,
                 entry,
                 connected,
                 description,
                 options
             )
             : TryStartConnectedSpawnRequestAsync(
                 caller,
                 node.Name,
                 description,
                 options
             );
        }
        ReferenceTerm reference = await operation ?? throw new InvalidOperationException(DistSpawn430Names.Pending);

        return request ? reference : Term.Tuple(reference, Term.A(options.Spawn.Monitor ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
    }
}

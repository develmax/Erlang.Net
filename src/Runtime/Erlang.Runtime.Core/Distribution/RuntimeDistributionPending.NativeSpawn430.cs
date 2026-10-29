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
        var input641 = RuntimeProcessPayload641.Prepare641(runtime, caller, Term.Tuple(options.Tag, options.Spawn.MonitorOptions.Tag));
        try
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
                    request,
                    input641
                );
                if (!entries.TryGetValue(node.Name, out var weak) || !weak.TryGetTarget(out var entry)) throw new NotSupportedException(DistSpawn430Names.Pending);
                if (entry.State == DistributionPendingState.Exiting) return runtime.FailNativeDistSpawn430(
                    caller,
                    options,
                    DistSpawn430Names.NoConnection,
                    options.Spawn.Monitor,
                    request,
                    input641
                );
                if (entry.Connected is not { } connected || !IsConnectedSignalGeneration(entry, connected, entry.ConnectionId)) throw new NotSupportedException(DistSpawn430Names.Pending);
                if ((connected.Flags & DistributionPassThroughFlags.Spawn) == 0) return runtime.FailNativeDistSpawn430(
                    caller,
                    options,
                    DistSpawn430Names.NotSupported,
                    options.Spawn.Monitor,
                    request,
                    input641
                );
                // Existing owner reserves the actual request/reference/output ticket synchronously
                // under this same gate; payload encoding and physical publication await outside.
                operation = caller.AsyncDistribution
                 ? StartNativeDistSpawnAsynchronously430(
                     caller,
                     entry,
                     connected,
                     description,
                     options,
                     input641
                 )
                 : TryStartConnectedSpawnRequestAsync(
                     caller,
                     node.Name,
                     description,
                     options,
                     input641
                 );
            }
            ReferenceTerm reference = await operation ?? throw new InvalidOperationException(DistSpawn430Names.Pending);

            return request ? reference : Term.Tuple(reference, Term.A(options.Spawn.Monitor ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));

        }
        finally
        {
            input641.Close641();
        }
    }
}

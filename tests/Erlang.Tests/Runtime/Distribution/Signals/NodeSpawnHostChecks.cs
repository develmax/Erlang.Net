namespace Erlang.Tests;

public static class NodeSpawnHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(NodeSpawnHostNames.Node, null, NodeSpawnHostParameters.Capacity);
        int invoked = 0;
        await runtime.InitializeModulesAsync(registry => registry.Register(
            NodeSpawnHostNames.Module,
            NodeSpawnHostNames.Callback,
            0,
            (_, _) =>
        {
            Interlocked.Increment(ref invoked);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        ));
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                for (int i = 0; i < NodeSpawnHostParameters.Attempts; i++)
                {
                    Term reply = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangModuleNames.SpawnMonitor,
                        Term.A(NodeSpawnHostNames.Missing),
                        Term.A(NodeSpawnHostNames.Module),
                        Term.A(NodeSpawnHostNames.Callback),
                        Nil.Value
                    );
                    var pair = (TupleTerm)reply;
                    var pid = (Pid)pair.Items[0];
                    var reference = (ReferenceTerm)pair.Items[1];
                    Term? down = await context.ReceiveAsync(
                        message => message is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(NodeSpawnHostNames.Down)) && tuple.Items[1].Equals(reference) ? tuple : null,
                        NodeSpawnHostParameters.Timeout
                    );
                    Require(down is TupleTerm result && result.Items[2].Equals(Term.A(NodeSpawnHostNames.Process)) && result.Items[3].Equals(pid) && result.Items[4].Equals(Term.A(NodeSpawnNames.Disconnected)) && !runtime.IsAlive(pid));
                }
                Require(invoked == 0);
                Term local = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.SpawnMonitor,
                    runtime.NodeAtom,
                    Term.A(NodeSpawnHostNames.Module),
                    Term.A(NodeSpawnHostNames.Callback),
                    Nil.Value
                );
                var localPair = (TupleTerm)local;
                var localReference = (ReferenceTerm)localPair.Items[1];
                await context.ReceiveAsync(
                    message => message is TupleTerm { Items.Count: 5 } tuple && tuple.Items[1].Equals(localReference) ? tuple : null,
                    NodeSpawnHostParameters.Timeout
                );
                Require(invoked == 1);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await process.Completion.WaitAsync(NodeSpawnHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NodeSpawnHostNames.Failure);
    }
}

namespace Erlang.Tests;

internal static class NodeMonitor458Checks
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var remote = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await remote.Initialize(ConnectedSpawnHostNames.NodeB);
        await local.Connect(remote);
        await using var watcher = new RuntimeNodeFoundationHostActor(local.Runtime);
        await Monitor(true);
        await Monitor(true);
        await Monitor(true);
        await Monitor(false);
        Require(await Receive(TimeSpan.Zero) is null);
        await local.Controller.DisposeAsync();
        Term down = Term.Tuple(Term.A(NodeMonitor458Names.Down), Term.A(remote.Runtime.Node));
        Require(down.Equals(await Receive(ConnectedSpawnHostNames.Deadline)));
        Require(down.Equals(await Receive(ConnectedSpawnHostNames.Deadline)));
        Require(await Receive(TimeSpan.Zero) is null);
        await local.Reconnect(remote);
        await Monitor(true);
        await Monitor(false);
        await local.Controller.DisposeAsync();
        Require(await Receive(TimeSpan.Zero) is null);
        await local.Reconnect(remote);
        await Monitor(true);
        Require(await Receive(TimeSpan.Zero) is null);
        await local.Controller.DisposeAsync();
        Require(down.Equals(await Receive(ConnectedSpawnHostNames.Deadline)));
        Require(await Receive(TimeSpan.Zero) is null);

        Task<Term> Monitor(bool enable) => watcher.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            NodeMonitor458Names.Monitor,
            Term.A(remote.Runtime.Node),
            Term.A(enable ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
        ));

        Task<Term?> Receive(TimeSpan timeout)
            => watcher.InvokeAsync(async context => await context.ReceiveAsync(value => value, timeout));
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NodeMonitor458Names.Failure);
    }
}

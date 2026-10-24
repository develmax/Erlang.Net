namespace Erlang.Tests;

internal static class SourceMonitor458Checks
{
    internal static async Task Run()
    {
        foreach (bool custom in new[] { false, true })
        {
            await using var local = new ConnectedSpawnHostEndpoint();
            await using var remote = new ConnectedSpawnHostEndpoint();
            await local.Initialize(ConnectedSpawnHostNames.NodeA);
            await remote.Initialize(ConnectedSpawnHostNames.NodeB);
            await local.Connect(remote);
            await remote.Connect(local);
            await using var watcher = new RuntimeNodeFoundationHostActor(local.Runtime);
            await using var target = new RuntimeNodeFoundationHostActor(remote.Runtime);
            var pid = target.Process.Pid;
            var foreign = new Pid(pid.Node, pid.Id, pid.Creation);
            Term tag = Term.A(custom ? NodeMonitor458Names.CustomTag : ConnectedSpawnHostNames.Down);
            var reference = (ReferenceTerm)await watcher.InvokeAsync(async context =>
            {
                Term[] arguments = custom
                    ? [Term.A(NodeMonitor458Names.Process), foreign, Term.List(Term.Tuple(Term.A(NodeMonitor458Names.Tag), tag))]
                    : [Term.A(NodeMonitor458Names.Process), foreign];

                return await local.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.Monitor,
                    arguments
                );
            });
            Require(await watcher.InvokeAsync(async context => await context.ReceiveAsync(value => value, TimeSpan.Zero)) is null);
            await remote.PutPacket(await local.TakePacket());
            await target.InvokeAsync(async context =>
            {
                _ = await context.ReceiveAsync(value => value, TimeSpan.Zero);

                return true;
            });
            await target.DisposeAsync();
            await local.PutPacket(await remote.TakePacket());
            await local.Controller.DisposeAsync();
            Term expected = Term.Tuple(
                tag,
                reference,
                Term.A(NodeMonitor458Names.Process),
                foreign,
                Term.A(ProcessExitReasons.Normal)
            );
            Term? actual = await watcher.InvokeAsync(async context => await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline));
            if (!expected.Equals(actual)) throw new InvalidOperationException(NodeMonitor458Names.SourceFailure + Term.Tuple(expected, actual ?? Nil.Value));
            Require(await watcher.InvokeAsync(async context => await context.ReceiveAsync(value => value, TimeSpan.Zero)) is null);
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NodeMonitor458Names.SourceFailure);
    }
}

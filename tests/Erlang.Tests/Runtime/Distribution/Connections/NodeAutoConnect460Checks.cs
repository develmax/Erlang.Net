namespace Erlang.Tests;

internal static class NodeAutoConnect460Checks
{
    internal static async Task Run()
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await local.Initialize(ConnectedSpawnHostNames.NodeA);
        await using var watcher = new RuntimeNodeFoundationHostActor(local.Runtime);
        Atom peer = Term.A(ConnectedSpawnHostNames.NodeB);
        Term down = Term.Tuple(Term.A(RuntimeNodeControlNames.NodeDown), peer);
        await Monitor(false);
        Require(await KernelMessage() is null);
        Require(await Receive() is null);
        await Monitor(true);
        await Monitor(true);
        TupleTerm first = Request(await KernelMessage());
        Require(await KernelMessage() is null);
        Require(await Receive() is null);
        await Monitor(false);
        Require((await Abort(first.Items[2])).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require(down.Equals(await Receive()));
        Require(await Receive() is null);
        await Monitor(true);
        TupleTerm second = Request(await KernelMessage());
        Require(!((TupleTerm)first.Items[2]).Items[0].Equals(((TupleTerm)second.Items[2]).Items[0]));
        Require((await Abort(first.Items[2])).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require(await Receive() is null);
        Require((await Abort(second.Items[2])).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require(down.Equals(await Receive()));
        Require(await Receive() is null);

        Task<Term> Monitor(bool enabled) => watcher.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            RuntimeNodeControlNames.Monitor,
            peer,
            Term.A(enabled ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
        ));

        Task<Term> Abort(Term handle) => local.Kernel.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            NodeAutoConnect460Names.Internal,
            NodeAutoConnect460Names.Abort,
            peer,
            handle
        ));

        Task<Term?> Receive() => watcher.InvokeAsync(async context => await context.ReceiveAsync(value => value, TimeSpan.Zero));

        Task<Term?> KernelMessage() => local.Kernel.InvokeAsync(async context => await context.ReceiveAsync(value => value, TimeSpan.Zero));

        TupleTerm Request(Term? message)
        {
            Require(message is TupleTerm { Items.Count: 3 } tuple && tuple.Items[0].Equals(Term.A(RuntimeNodeControlNames.AutoConnect))
                && tuple.Items[1].Equals(peer) && tuple.Items[2] is TupleTerm { Items.Count: 2 });

            return (TupleTerm)message!;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NodeAutoConnect460Names.Failure);
    }
}

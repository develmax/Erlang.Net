namespace Erlang.Tests;

public static class RuntimeConnectedNodeQueryHostChecks
{
    public static async Task Run()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b, ConnectedSpawnHostNames.Flags | RuntimeConnectedNodeQueryHostNames.Published);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);

        Task<Term> Query(params Term[] arguments) => caller.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeNodeQueryNames.Nodes,
                arguments
            ));
        Term infoOptions = new MapTerm([
            new(Term.A(RuntimeNodeQueryNames.ConnectionId), Term.A(ErlangBooleanAtoms.True)),
            new(Term.A(RuntimeNodeQueryNames.NodeType), Term.A(ErlangBooleanAtoms.True))]);
        Term node = Term.A(b.Runtime.Node);
        Require((await Query()).Equals(Term.List(node)));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Hidden))).Equals(Nil.Value));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Connected))).Equals(Term.List(node)));
        uint firstId = (uint)((Integer)a.Handle!.Items[0]).Value;
        Require((await Query(Term.A(RuntimeNodeQueryNames.Visible), infoOptions)).Equals(
            Term.List(Term.Tuple(node, Info(firstId, RuntimeNodeQueryNames.Visible)))));
        Require((await Query(Term.A(RuntimeNodeQueryNames.This), infoOptions)).Equals(
            Term.List(Term.Tuple(a.Runtime.NodeAtom, Info(null, RuntimeNodeQueryNames.This)))));
        var known = Cons.Items(await Query(Term.List(Term.A(RuntimeNodeQueryNames.Known),
            Term.A(RuntimeNodeQueryNames.Connected), Term.A(RuntimeNodeQueryNames.This)))).ToArray();
        Require(known.Length == known.Distinct().Count() && known.Contains(node) && known.Contains(a.Runtime.NodeAtom));
        await a.Controller.DisposeAsync();
        Require((await Query()).Equals(Nil.Value));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Connected))).Equals(Nil.Value));
        var retired = Cons.Items(await Query(Term.A(RuntimeNodeQueryNames.Known), infoOptions)).ToArray();
        Require(retired.Contains(Term.Tuple(node, Info(null, RuntimeNodeQueryNames.Known))));
        await a.Reconnect(b);
        uint nextId = (uint)((Integer)a.Handle!.Items[0]).Value;
        Require(firstId != nextId);
        Require((await Query()).Equals(Nil.Value));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Hidden), infoOptions)).Equals(
            Term.List(Term.Tuple(node, Info(nextId, RuntimeNodeQueryNames.Hidden)))));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Connected))).Equals(Term.List(node)));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Connected), new MapTerm([]))).Equals(
            Term.List(Term.Tuple(node, new MapTerm([])))));
        await a.Kernel.DisposeAsync();
        Require((await Query(Term.A(RuntimeNodeQueryNames.This))).Equals(Term.List(a.Runtime.NodeAtom)));
        Require((await Query(Term.A(RuntimeNodeQueryNames.Connected))).Equals(Nil.Value));
    }

    private static MapTerm Info(uint? id, string type) => new([
        new(Term.A(RuntimeNodeQueryNames.ConnectionId), id is uint value ? Term.I(value) : Term.A(RuntimeNodeQueryNames.Undefined)),
        new(Term.A(RuntimeNodeQueryNames.NodeType), Term.A(type))]);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(RuntimeConnectedNodeQueryHostNames.Failure);
    }
}

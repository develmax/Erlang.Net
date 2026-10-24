namespace Erlang.Tests;

internal static class NodeMonitor459Checks
{
    internal static async Task Run()
    {
        var results = new List<Term>();
        foreach (var profile in new[] { (2, 1, 0, 0), (3, 3, 0, 0), (3, 1, 0, 0), (2, 3, 0, 0), (5, 0, 0, 0) })
        {
            results.Add(await Observe(profile));
        }
        byte[] expected = NetKernel400HostAccess.Resource(NodeMonitor459Names.Resource);
        Term actual = Term.Tuple(Term.A(NodeMonitor459Names.Ok), Cons.From(results));
        Require(ExternalTermFormat.Encode(actual).SequenceEqual(expected));
        Term down = Term.Tuple(Term.A(NodeMonitor458Names.Down), Term.A(NodeMonitor459Names.Node));
        Require((await Observe((3, 1, 2, 1))).Equals(Term.Tuple(
            Term.Tuple(
                Term.I(3),
                Term.I(1),
                Term.I(2),
                Term.I(1)
            ),
            Cons.From(new[] { down, down }),
            Cons.From(new[] { down })
        )));
        Require((await Observe((2, 3, 2, 2))).Equals(Term.Tuple(
            Term.Tuple(
                Term.I(2),
                Term.I(3),
                Term.I(2),
                Term.I(2)
            ),
            Nil.Value,
            Nil.Value
        )));

    }

    private static async Task<Term> Observe((int, int, int, int) profile)
    {
        await using var local = new ConnectedSpawnHostEndpoint();
        await using var remote = new ConnectedSpawnHostEndpoint();
        await local.Initialize(NodeMonitor459Names.Owner);
        await remote.Initialize(NodeMonitor459Names.Node);
        await local.Connect(remote);
        await using var watcher = new RuntimeNodeFoundationHostActor(local.Runtime);
        await SetTrace();
        await Repeat(true, profile.Item1);
        await local.Controller.DisposeAsync();
        if (profile.Item3 > 0) await local.Reconnect(remote);
        await Repeat(false, profile.Item2);
        await Repeat(true, profile.Item3);
        await Repeat(false, profile.Item4);
        Term boundary = Term.A(NodeMonitor459Names.Boundary);
        local.Runtime.Send(watcher.Process.Pid, boundary);
        var oldMessages = new List<Term>();
        while (true)
        {
            Term? message = await Receive();
            Require(message is not null);
            if (boundary.Equals(message)) break;
            oldMessages.Add(message!);
        }
        await SetTrace();
        if (profile.Item3 > 0) await local.Controller.DisposeAsync();
        var newMessages = new List<Term>();
        while (await Receive() is { } message) newMessages.Add(message);

        return Term.Tuple(
            Term.Tuple(
                Term.I(profile.Item1),
                Term.I(profile.Item2),
                Term.I(profile.Item3),
                Term.I(profile.Item4)
            ),
            Cons.From(oldMessages),
            Cons.From(newMessages)
        );

        async Task Repeat(bool enable, int count)
        {
            for (int index = 0; index < count; index++)
                await watcher.InvokeAsync(async context => await local.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    NodeMonitor458Names.Monitor,
                    Term.A(remote.Runtime.Node),
                    Term.A(enable ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
                ));
        }

        Task<Term> SetTrace() => watcher.InvokeAsync(async context => await local.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            SequenceTraceNames.Set,
            Term.A(SequenceTraceNames.Label),
            Term.I(NodeMonitor459Names.TraceLabel)
        ));

        Task<Term?> Receive() => watcher.InvokeAsync(async context =>
        {
            async Task<Term> Token() => Term.Tuple(
                await local.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Info,
                    Term.A(SequenceTraceNames.Label)
                ),
                await local.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Info,
                    Term.A(SequenceTraceNames.Serial)
                )
            );
            Term before = await Token();
            Term? message = await context.ReceiveAsync(value => value, TimeSpan.Zero);
            if (message is TupleTerm) Require(before.Equals(await Token()));

            return message;
        });
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NodeMonitor459Names.Failure);
    }
}

namespace Erlang.Tests;

internal static class DistChannel429HostChecks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(DistChannel429HostNames.Failure);
    }

    private static TupleTerm Handle(Term value)
    {
        Require(value is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(Term.A(DistChannel429HostNames.Ok)) && t.Items[1] is TupleTerm { Items.Count: 2 } h && h.Items[0] is Integer && h.Items[1] is ResourceReferenceTerm);

        return (TupleTerm)((TupleTerm)value).Items[1];
    }

    internal static async Task Legacy()
    {
        await using var host = await DistChannel429Host.Create();
        ProcessRuntime runtime = host.Runtime;
        await host.Controller!.InvokeAsync(async c =>
        {
            TupleTerm handle = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
            runtime.DistributionPending.AdoptConnected(
                handle,
                c,
                DistributionCapabilityProfile429.Preferred,
                1
            );
            Require(handle.Items[1] is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.DistributionEntry });
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Get,
                handle,
                Term.A(DistChannel429HostNames.Size)
            )).Equals(Term.A(DistChannel429HostNames.False)));
            _ = await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Input,
                handle,
                c.Self
            );
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Put,
                handle,
                Nil.Value
            )).Equals(Term.A(DistChannel429HostNames.Ok)));
            Require(DistributionInputAdapter.PutData(c, handle, new BitString(Array.Empty<byte>())).Equals(Term.A(DistChannel429HostNames.Ok)));
            Require(await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Statistics,
                handle
            ) is TupleTerm { Items.Count: 4 });
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Data,
                handle
            )).Equals(Term.A(DistChannel429HostNames.None)));

            return true;
        });
    }

    internal static async Task Same()
    {
        await using var host = await DistChannel429Host.Create();
        ProcessRuntime runtime = host.Runtime;
        Pid kernel = (await host.Kernel!.Ready).Self;
        await host.Controller!.InvokeAsync(async c =>
        {
            _ = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
            TupleTerm handle = Handle(await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Remote, c.Self));
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Get,
                handle,
                Term.A(DistChannel429HostNames.Size)
            )).Equals(Term.A(DistChannel429HostNames.False)));
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Data,
                handle
            )).Equals(Term.A(DistChannel429HostNames.None)));
            _ = await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Input,
                handle,
                c.Self
            );
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Put,
                handle,
                Nil.Value
            )).Equals(Term.A(DistChannel429HostNames.Ok)));
            runtime.Unregister(DistChannel429HostNames.Kernel);
            try
            {
                Require((await runtime.Modules.Call(
                    c,
                    DistChannel429HostNames.Runtime,
                    DistChannel429HostNames.Get,
                    handle,
                    Term.A(DistChannel429HostNames.Size)
                )).Equals(Term.A(DistChannel429HostNames.False)));
            }
            finally
            {
                runtime.Register(DistChannel429HostNames.Kernel, kernel);
            }
            _ = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Other));
            Require((await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Other, c.Self)).Equals(Term.A(DistChannel429HostNames.BadArgument)));

            return true;
        });
    }

    internal static async Task Cross()
    {
        foreach (bool kill in new[] { false, true })
        {
            await using var host = await DistChannel429Host.Create();
            ProcessRuntime runtime = host.Runtime;
            ProcessContext target = await host.Controller!.Ready;
            _ = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Task<bool>? held = null;
            try
            {
                held = host.Controller!.InvokeAsync(async c =>
                {
                    entered.Set();
                    Require(release.Wait(DistChannel429HostNames.Deadline));
                    try
                    {
                        await c.Mailbox.ReceiveAsync<Term>(_ => null, TimeSpan.Zero, c.Cancellation);
                    }
                    catch (OperationCanceledException) when (kill && c.Cancellation.IsCancellationRequested) { }

                    return true;
                });
                Require(entered.Wait(DistChannel429HostNames.Deadline));
                ReferenceTerm reply = await host.Requester!.InvokeAsync(async c =>
                {
                    Term result = await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Remote, target.Self);
                    Require(result is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(Term.A(DistChannel429HostNames.Message)) && t.Items[1] is ReferenceTerm);
                    var id = (ReferenceTerm)((TupleTerm)result).Items[1];
                    Require(await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm x && x.Items[0].Equals(id) ? v : null, TimeSpan.Zero, c.Cancellation) is null);
                    if (kill) runtime.Exit(c, target.Self, Term.A(DistChannel429HostNames.Kill));

                    return id;
                });
                release.Set();
                Require(await held);
                Term? response = await host.Requester!.InvokeAsync(async c => await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(reply) ? v : null, DistChannel429HostNames.Deadline, c.Cancellation));
                Require(response is TupleTerm responseTuple && (kill ? responseTuple.Items[1].Equals(Term.A(DistChannel429HostNames.BadArgument)) : responseTuple.Items[1] is TupleTerm { Items.Count: 2 } h && h.Items[0] is Integer && h.Items[1] is ResourceReferenceTerm));
            }
            finally
            {
                release.Set();
                if (held is not null) await held;
            }
        }
    }

    internal static async Task Generation()
    {
        await using var host = await DistChannel429Host.Create();
        ProcessRuntime runtime = host.Runtime;
        _ = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
        TupleTerm stale = await host.Controller!.InvokeAsync(async c => Handle(await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Remote, c.Self)));
        await host.Controller!.DisposeAsync();
        await using var next = new RuntimeNodeFoundationHostActor(runtime);
        _ = runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
        await using var other = await DistChannel429Host.Create();
        _ = other.Runtime.DistributionPending.BeginPending(Term.A(DistChannel429HostNames.Remote));
        TupleTerm foreign = await other.Controller!.InvokeAsync(async c => Handle(await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Remote, c.Self)));
        await next.InvokeAsync(async c =>
        {
            TupleTerm current = Handle(await DistChannel429Host.CreateChannel(c, DistChannel429HostNames.Remote, c.Self));
            Require((await runtime.Modules.Call(
                c,
                DistChannel429HostNames.Runtime,
                DistChannel429HostNames.Get,
                current,
                Term.A(DistChannel429HostNames.Size)
            )).Equals(Term.A(DistChannel429HostNames.False)));
            foreach (Term invalid in new Term[] { stale, foreign, runtime.MakeReference(), current.Items[1], Term.Tuple(Term.I(-1), current.Items[1]) })
            {
                bool badarg = false;
                try
                {
                    await runtime.Modules.Call(
                        c,
                        DistChannel429HostNames.Runtime,
                        DistChannel429HostNames.Get,
                        invalid,
                        Term.A(DistChannel429HostNames.Size)
                    );
                }
                catch (ErlangException error)
                {
                    badarg = error.Reason.Equals(Term.A(DistChannel429HostNames.BadArgument));
                }
                catch (InvalidOperationException) when (ReferenceEquals(invalid, foreign))
                {
                    badarg = true;
                }
                Require(badarg);
            }

            return true;
        });
    }
}

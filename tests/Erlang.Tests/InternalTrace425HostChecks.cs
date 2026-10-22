namespace Erlang.Tests;

internal static class InternalTrace425HostChecks
{
    private static void Require(bool value) => InternalTrace425HostAccess.Require(value);

    internal static async Task Events()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapInternalTrace425.Register(runtime, registry);
            RuntimeBootstrapInternalTrace425.Register(runtime, registry);
        });
        await using var control = new RuntimeNodeFoundationHostActor(runtime);
        await using var one = new RuntimeNodeFoundationHostActor(runtime);
        await using var two = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext first = await one.Ready, second = await two.Ready, source = await target.Ready;
        Term a = await InternalTrace425HostAccess.Create(control, first.Self), b = await InternalTrace425HostAccess.Create(control, second.Self);
        foreach (Term session in new[] { a, b })
            Require((await InternalTrace425HostAccess.Call(
                control,
                InternalTrace425HostNames.Trace,
                session,
                source.Self,
                Term.A(InternalTrace425HostNames.True),
                Term.List(Term.A(InternalTrace425HostNames.Send))
            )).Equals(Term.I(1)));
        Require((await control.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalTrace425HostNames.Runtime,
            InternalTrace425HostNames.Info,
            source.Self,
            Term.A(InternalTrace425HostNames.Flags)
        ))).Equals(Term.Tuple(Term.A(InternalTrace425HostNames.Flags), Nil.Value)));
        Term firstMessage = Term.A(InternalTrace425HostNames.First);
        await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalTrace425HostNames.Runtime,
            InternalTrace425HostNames.Send,
            first.Self,
            firstMessage
        ));
        Term expected = Term.Tuple(
            Term.A(InternalTrace425HostNames.Message),
            source.Self,
            Term.A(InternalTrace425HostNames.Send),
            firstMessage,
            first.Self
        );
        Require((await InternalTrace425HostAccess.ReceiveTrace(one, InternalTrace425HostNames.Deadline))!.Equals(expected));
        Require((await InternalTrace425HostAccess.ReceiveTrace(two, InternalTrace425HostNames.Deadline))!.Equals(expected));
        Require((await InternalTrace425HostAccess.Call(control, InternalTrace425HostNames.Destroy, a)).Equals(Term.A(InternalTrace425HostNames.True)));
        Require((await InternalTrace425HostAccess.Call(control, InternalTrace425HostNames.Destroy, a)).Equals(Term.A(InternalTrace425HostNames.False)));
        Term next = Term.A(InternalTrace425HostNames.Second);
        await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalTrace425HostNames.Runtime,
            InternalTrace425HostNames.Send,
            first.Self,
            next
        ));
        Require(await InternalTrace425HostAccess.ReceiveTrace(one, TimeSpan.Zero) is null);
        Require((await InternalTrace425HostAccess.ReceiveTrace(two, InternalTrace425HostNames.Deadline))!.Equals(
            Term.Tuple(
                Term.A(InternalTrace425HostNames.Message),
                source.Self,
                Term.A(InternalTrace425HostNames.Send),
                next,
                first.Self
            )));
        Require((await InternalTrace425HostAccess.Call(control, InternalTrace425HostNames.Destroy, b)).Equals(Term.A(InternalTrace425HostNames.True)));
        Require(InternalTrace425HostAccess.SessionCount(runtime) == 0);
    }

    internal static async Task Calls()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(runtime, registry));
        await using var control = new RuntimeNodeFoundationHostActor(runtime);
        await using var one = new RuntimeNodeFoundationHostActor(runtime);
        await using var two = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext first = await one.Ready, second = await two.Ready, source = await target.Ready;
        Term a = await InternalTrace425HostAccess.Create(control, first.Self), b = await InternalTrace425HostAccess.Create(control, second.Self);
        Term mfa = Term.Tuple(Term.A(InternalTrace425HostNames.Runtime), Term.A(InternalTrace425HostNames.Abs), Term.I(1));
        Term match = Term.List(Term.Tuple(
            Term.A(InternalTrace425HostNames.Wildcard),
            Nil.Value,
            Term.List(Term.Tuple(Term.A(InternalTrace425HostNames.ReturnTrace)))
        ));
        foreach (Term session in new[] { a, b })
        {
            Require((await InternalTrace425HostAccess.Call(
                control,
                InternalTrace425HostNames.Trace,
                session,
                source.Self,
                Term.A(InternalTrace425HostNames.True),
                Term.List(Term.A(InternalTrace425HostNames.Call))
            )).Equals(Term.I(1)));
            Require((await InternalTrace425HostAccess.Call(
                control,
                InternalTrace425HostNames.Pattern,
                session,
                mfa,
                match,
                Nil.Value
            )).Equals(Term.I(1)));
        }
        Require((await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalTrace425HostNames.Runtime,
            InternalTrace425HostNames.Abs,
            Term.I(-9)
        ))).Equals(Term.I(9)));
        foreach (var tracer in new[] { one, two })
        {
            Require((await InternalTrace425HostAccess.ReceiveTrace(tracer, InternalTrace425HostNames.Deadline))!.Equals(
                Term.Tuple(
                    Term.A(InternalTrace425HostNames.Message),
                    source.Self,
                    Term.A(InternalTrace425HostNames.Call),
                    Term.Tuple(Term.A(InternalTrace425HostNames.Runtime), Term.A(InternalTrace425HostNames.Abs), Term.List(Term.I(-9)))
                )));
            Require((await InternalTrace425HostAccess.ReceiveTrace(tracer, InternalTrace425HostNames.Deadline))!.Equals(
                Term.Tuple(
                    Term.A(InternalTrace425HostNames.Message),
                    source.Self,
                    Term.A(InternalTrace425HostNames.Return),
                    mfa,
                    Term.I(9)
                )));
        }
        await InternalTrace425HostAccess.Call(control, InternalTrace425HostNames.Destroy, a);
        await InternalTrace425HostAccess.Call(control, InternalTrace425HostNames.Destroy, b);
    }

    internal static async Task Lifetime()
    {
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(runtime, registry));
            actor = new RuntimeNodeFoundationHostActor(runtime);
            var dropped = await InternalTrace425HostAccess.CreateWeak(actor);
            await actor.Invoke(_ => true); // Release the actor loop's preceding command graph.
            for (int index = 0; index < 20 && InternalTrace425HostAccess.SessionCount(runtime) != 0; index++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(10);
            }
            Require(!dropped.Resource.TryGetTarget(out _) && InternalTrace425HostAccess.SessionCount(runtime) == 0);
            bool stale = false;
            try
            {
                await InternalTrace425HostAccess.Call(actor, InternalTrace425HostNames.Destroy, dropped.Weak);
            }
            catch (ErlangException error)
            {
                stale = error.Reason.Equals(Term.A(InternalTrace425HostNames.BadArgument));
            }
            Require(stale);
            Term retained = await InternalTrace425HostAccess.Create(actor, Term.A(InternalTrace425HostNames.Undefined));
            Require(InternalTrace425HostAccess.SessionCount(runtime) == 1);
            await actor.DisposeAsync();
            actor = null;
            await runtime.DisposeAsync();
            Require(InternalTrace425HostAccess.SessionCount(runtime) == 0 && retained is TupleTerm);
        }
        finally
        {
            if (actor is not null) await actor.DisposeAsync();
            await runtime.DisposeAsync();
        }
    }
}

using System.Numerics;

namespace Erlang.Tests;

internal static class DebugDirty424Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(DebugDirty424Names.Failure);
    }

    private static ValueTask<Term> Call(
        ProcessContext context,
        string route,
        Term operation,
        Term argument
    )
        => route is DebugDirty424Names.Cpu or DebugDirty424Names.Io
            ? context.Runtime.Modules.Call(
                context,
                DebugDirty424Names.Module,
                route,
                operation,
                argument
            )
            : context.Runtime.Modules.Call(
                context,
                DebugDirty424Names.Module,
                DebugDirty424Names.Dispatch,
                Term.A(route),
                operation,
                argument
            );

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDirty424.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string route in new[] { DebugDirty424Names.Cpu, DebugDirty424Names.Io, DebugDirty424Names.Normal })
            {
                Require((await Call(
                    context,
                    route,
                    Term.A("scheduler"),
                    Term.A("type")
                )).Equals(Term.A(route)));
                foreach (int count in new[] { 0, 2, 4, 8, 1000 })
                    Require((await Call(
                        context,
                        route,
                        Term.A("reschedule"),
                        Term.I(count)
                    )).Equals(Term.A("ok")));
                foreach (string reason in new[] { "notsup", "undef", "badarith", "noproc", "system_limit", "badarg" })
                {
                    bool rejected = false;
                    try
                    {
                        await Call(
                            context,
                            route,
                            Term.A("error"),
                            Term.A(reason)
                        );
                    }
                    catch (ErlangException error)
                    {
                        rejected = error.Reason.Equals(Term.A(reason));
                        var frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                        Require(frame.Items[0].Equals(Term.A(DebugDirty424Names.Module)));
                        Require(frame.Items[1].Equals(Term.A(route == DebugDirty424Names.Normal ? DebugDirty424Names.Dispatch : route)));
                    }
                    Require(rejected);
                }
                bool odd = false;
                try
                {
                    await Call(
                        context,
                        route,
                        Term.A("reschedule"),
                        Term.I(9)
                    );
                }
                catch (ErlangException error)
                {
                    var first = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                    Term expected = route == DebugDirty424Names.Normal
                        ? Term.List(Term.A(route), Term.A("reschedule"), Term.I(9))
                        : Term.List(Term.A("reschedule"), Term.I(9));
                    odd = error.Reason.Equals(Term.A("badarg")) && first.Items[2].Equals(expected);
                }
                Require(odd);
                Require((await Call(
                    context,
                    route,
                    Term.A("wait"),
                    new Integer(long.MinValue)
                )).Equals(Term.A("ok")));
                Require((await Call(
                    context,
                    route,
                    Term.A("send"),
                    Term.A("not_a_pid")
                )).Equals(Term.A("ok")));
            }

            return true;
        });
    }

    internal static async Task Copies()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDirty424.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var child = Term.Tuple(Term.I(42), Term.I(43));
            var resource = runtime.ReferenceResources.CreateBinary(new byte[] { 4, 5, 6 });
            Term original = Term.Tuple(
                child,
                child,
                resource,
                new BitString(new byte[128])
            );
            Term[] copies = Cons.Items(await Call(
                context,
                DebugDirty424Names.Cpu,
                Term.A("copy"),
                original
            )).ToArray();
            Require(copies.Length == 1000 && copies.All(copy => copy.Equals(original)));
            Require(!ReferenceEquals(copies[0], original) && !ReferenceEquals(copies[0], copies[1]));
            var first = (TupleTerm)copies[0];
            Require(!ReferenceEquals(first.Items[0], first.Items[1]));
            Require(first.Items[2] is ResourceReferenceTerm && !ReferenceEquals(first.Items[2], resource));
            using var lease = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)first.Items[2]);
            Require(lease is not null && lease.Value.Equals(new BitString(new byte[] { 4, 5, 6 })));
            Term deep = Nil.Value;
            for (int index = 0; index < 512; index++) deep = new Cons(Term.I(index), deep);
            Term[] deepCopies = Cons.Items(await Call(
                context,
                DebugDirty424Names.Io,
                Term.A("copy"),
                deep
            )).ToArray();
            Require(deepCopies.Length == 1000 && Cons.Items(deepCopies[0]).Count() == 512);
            var external = runtime.Functions.External(runtime, new ExternalFunctionIdentity(
                Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0));
            Term[] functions = Cons.Items(await Call(
                context,
                DebugDirty424Names.Cpu,
                Term.A("copy"),
                external
            )).ToArray();
            Require(functions.Length == 1000 && functions.All(copy => copy.Equals(external)));
            Require(!ReferenceEquals(functions[0], external) && !ReferenceEquals(functions[0], functions[1]));
            Require((await ((FunctionTerm)functions[0]).Invoke(context, [])).Equals(context.Self));
            var copiedAgain = (FunctionTerm)Cons.Items(await Call(
                context,
                DebugDirty424Names.Io,
                Term.A("copy"),
                functions[0]
            )).First();
            Require(!ReferenceEquals(copiedAgain, functions[0]) && copiedAgain.Equals(external));
            Require((await copiedAgain.Invoke(context, [])).Equals(context.Self));

            return true;
        });
    }

    internal static async Task Reschedule()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDirty424.Register(runtime, registry));
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var blocker = new RuntimeNodeFoundationHostActor(runtime);
        await using var normal = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext owner = await target.Ready;
        _ = await blocker.Ready;
        _ = await normal.Ready;
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var normalEntered = new ManualResetEventSlim();
        using var normalRelease = new ManualResetEventSlim();
        Task<bool>? held = null, normalHeld = null;
        Task<Term>? work = null;
        try
        {
            held = blocker.InvokeAsync(async context =>
            {
                await DirtyNative419Access.Schedule(
                    context,
                    DirtyNative419Names.Io,
                    [],
                    (_, _) =>
                {
                    entered.Set();
                    Require(release.Wait(DebugDirty424Names.Deadline));

                    return Nil.Value;
                }
                );

                return true;
            });
            Require(entered.Wait(DebugDirty424Names.Deadline));
            work = target.InvokeAsync(context => Call(
                context,
                DebugDirty424Names.Cpu,
                Term.A("reschedule"),
                Term.I(8)
            ).AsTask());
            await DebugDirtyQueue424Access.Until(() => DebugDirtyQueue424Access.QueuedDirty(runtime, owner, "io"));
            Require(!work.IsCompleted);
            normalHeld = normal.InvokeAsync(context =>
            {
                normalEntered.Set();
                Require(normalRelease.Wait(DebugDirty424Names.Deadline));

                return Task.FromResult(true);
            });
            Require(normalEntered.Wait(DebugDirty424Names.Deadline));
            release.Set();
            await DebugDirtyQueue424Access.Until(() => DebugDirtyQueue424Access.QueuedNormal(runtime, owner));
            Require(!work.IsCompleted && !DebugDirtyQueue424Access.PhysicallyDirty(runtime, owner));
            normalRelease.Set();
            Require((await work).Equals(Term.A("ok")));
            await held;
            await normalHeld;

            entered.Reset();
            release.Reset();
            held = blocker.InvokeAsync(async context =>
            {
                await DirtyNative419Access.Schedule(
                    context,
                    DirtyNative419Names.Cpu,
                    [],
                    (_, _) =>
                {
                    entered.Set();
                    Require(release.Wait(DebugDirty424Names.Deadline));

                    return Nil.Value;
                }
                );

                return true;
            });
            Require(entered.Wait(DebugDirty424Names.Deadline));
            work = target.InvokeAsync(context => Call(
                context,
                DebugDirty424Names.Normal,
                Term.A("reschedule"),
                Term.I(4)
            ).AsTask());
            await DebugDirtyQueue424Access.Until(() => DebugDirtyQueue424Access.QueuedDirty(runtime, owner, "cpu"));
            Require(!work.IsCompleted && !DebugDirtyQueue424Access.PhysicallyDirty(runtime, owner));
            release.Set();
            Require((await work).Equals(Term.A("ok")));
            await held;
        }
        finally
        {
            release.Set();
            normalRelease.Set();
            var cleanup = new List<Task>();
            if (held is not null) cleanup.Add(held);
            if (normalHeld is not null) cleanup.Add(normalHeld);
            if (work is not null) cleanup.Add(work);
            await Task.WhenAll(cleanup);
        }
    }

    internal static async Task Messages()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDirty424.Register(runtime, registry));
        await using var worker = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext target = await receiver.Ready, sender = await worker.Ready;
        Require((await worker.InvokeAsync(context => Call(
            context,
            DebugDirty424Names.Cpu,
            Term.A("send"),
            target.Self
        ).AsTask())).Equals(Term.A("ok")));
        Term? message = await receiver.InvokeAsync(context => context.Mailbox.ReceiveAsync<Term>(item => item, DebugDirty424Names.Deadline, context.Cancellation).AsTask());
        Require(message!.Equals(Term.Tuple(Term.A("ok"), sender.Self)));
        Task<Term> pending = worker.InvokeAsync(context => Call(
            context,
            DebugDirty424Names.Io,
            Term.A("ready_wait6_done"),
            target.Self
        ).AsTask());
        Term? ready = await receiver.InvokeAsync(context => context.Mailbox.ReceiveAsync<Term>(item => item, DebugDirty424Names.Deadline, context.Cancellation).AsTask());
        Require(ready!.Equals(Term.Tuple(Term.A("ready"), sender.Self)) && !pending.IsCompleted);
        Term? done = await receiver.InvokeAsync(context => context.Mailbox.ReceiveAsync<Term>(item => item, DebugDirty424Names.Deadline, context.Cancellation).AsTask());
        Require(done!.Equals(Term.Tuple(Term.A("done"), sender.Self)));
        Require((await pending).Equals(Term.A("ok")));
    }

    internal static async Task Exiting()
    {
        foreach (string route in new[] { DebugDirty424Names.Cpu, DebugDirty424Names.Io })
            foreach (string operation in new[] { "alive_waitexiting", "alive_waitexitingonly" })
            {
                await using var runtime = new ProcessRuntime(2);
                await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugDirty424.Register(runtime, registry));
                await using var worker = new RuntimeNodeFoundationHostActor(runtime);
                await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
                ProcessContext target = await receiver.Ready, owner = await worker.Ready;
                Task<Term> invocation = await worker.Invoke(context => Call(
                    context,
                    route,
                    Term.A(operation),
                    target.Self
                ).AsTask());
                Term? alive = await receiver.InvokeAsync(context => context.Mailbox.ReceiveAsync<Term>(item => item, DebugDirty424Names.Deadline, context.Cancellation).AsTask());
                Require(alive!.Equals(Term.Tuple(Term.A("alive"), owner.Self)));
                // Observe the real worker outcome separately from guest cancellation.
                var physical = DebugDirtyQueue424Access.PhysicalWork(runtime, owner);
                await receiver.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    DebugDirty424Names.ExitFunction,
                    owner.Self,
                    Term.A(DebugDirty424Names.KillSignal)
                ));
                bool canceled = false;
                try
                {
                    await invocation.WaitAsync(DebugDirty424Names.Deadline);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                Require(canceled);
                Term result = await physical.Outcome.WaitAsync(DebugDirty424Names.Deadline);
                await physical.Settled.WaitAsync(DebugDirty424Names.Deadline);
                if (operation == "alive_waitexitingonly") Require(result.Equals(Term.A("ok")));
                else
                {
                    var pair = (TupleTerm)result;
                    Require(pair.Items[0].Equals(Term.A("ok")));
                    BigInteger expected = BigInteger.Zero;
                    for (int index = 0; index < 9999; index++) expected = (expected << 64) + 4711;
                    Require(((Integer)pair.Items[1]).Value == expected);
                }
                Require((await worker.Process.Completion.WaitAsync(DebugDirty424Names.Deadline)).Equals(Term.A(DebugDirty424Names.KilledReason)));
            }
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugDirty424.Register(runtime, registry);
            RuntimeBootstrapDebugDirty424.Register(runtime, registry);
        });
        Require(DebugDirtyNative424Access.IsNativeEntry(
            runtime.Modules,
            DebugDirty424Names.Module,
            DebugDirty424Names.Cpu,
            2
        ));
        Require(DebugDirtyNative424Access.IsNativeEntry(
            runtime.Modules,
            DebugDirty424Names.Module,
            DebugDirty424Names.Io,
            2
        ));
        Require(DebugDirtyNative424Access.IsNativeEntry(
            runtime.Modules,
            DebugDirty424Names.Module,
            DebugDirty424Names.Dispatch,
            3
        ));
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool refused = false;
        try
        {
            RuntimeBootstrapDebugDirty424.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
    }
}

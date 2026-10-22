using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsHeir419HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsHeir419HostNames.Failure);
    }

    private static void Register(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsHeir419.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsRename418.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime r,
        ProcessContext c,
        string f,
        params Term[] a
    ) => r.Modules.Call(
        c,
        EtsHeir419HostNames.Ets,
        f,
        a
    );

    private static Term Gift() => Term.Tuple(Term.A(EtsHeir419HostNames.Data), Term.List(Term.I(EtsHeir419HostNames.Value)));

    private static bool Tag(Term value, string tag) => value is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].Equals(Term.A(tag));

    private static Task<Term?> Receive(ProcessContext context, string tag, TimeSpan timeout) => context.Mailbox.ReceiveAsync<Term>(value => Tag(value, tag) ? value : null, timeout, context.Cancellation).AsTask();

    private static async Task BadArgument(Func<Task<Term>> operation)
    {
        bool failed = false;
        try
        {
            await operation();
        }
        catch (ErlangException e)
        {
            failed = e.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(failed);
    }

    internal static async Task Lifetime()
    {
        foreach (bool message in new[] { true, false })
        {
            await using var runtime = new ProcessRuntime();
            Register(runtime);
            await using var owner = new RuntimeNodeFoundationHostActor(runtime);
            await using var heir = new RuntimeNodeFoundationHostActor(runtime);
            Term table = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.New,
                Term.A(EtsHeir419HostNames.Table),
                Term.List(Term.A(EtsHeir419HostNames.Named), Term.A(EtsHeir419HostNames.Private))
            ));
            Term gift = Gift();
            await owner.InvokeAsync(async c =>
            {
                _ = await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Insert,
                    table,
                    Term.Tuple(Term.I(EtsHeir419HostNames.Key), Term.I(EtsHeir419HostNames.Value))
                );

                return await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Setopts,
                    table,
                    message ? Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid, gift) : Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid)
                );
            });
            ReferenceTerm monitor = await heir.Invoke(c => runtime.Monitor(c, owner.Process.Pid));
            await heir.InvokeAsync(async c =>
            {
                runtime.Exit(c, owner.Process.Pid, Term.A(EtsHeir419HostNames.Kill));
                Term? first = await c.Mailbox.ReceiveAsync<Term>(
                    v => Tag(v, EtsHeir419HostNames.Transfer) || Tag(v, EtsHeir419HostNames.Down) ? v : null,
                    EtsHeir419HostNames.Deadline,
                    c.Cancellation
                );
                if (message)
                {
                    Require(first is TupleTerm { Items.Count: 4 } transfer && transfer.Items[0].Equals(Term.A(EtsHeir419HostNames.Transfer)) && transfer.Items[1].Equals(table) && transfer.Items[2].Equals(owner.Process.Pid) && transfer.Items[3].Equals(gift) && !ReferenceEquals(transfer.Items[3], gift));
                    first = await Receive(c, EtsHeir419HostNames.Down, EtsHeir419HostNames.Deadline);
                }
                Require(first is TupleTerm { Items.Count: 5 } down && down.Items[0].Equals(Term.A(EtsHeir419HostNames.Down)) && down.Items[1].Equals(monitor));
                Require(await Receive(c, EtsHeir419HostNames.Transfer, TimeSpan.Zero) is null);
                Require((await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Lookup,
                    table,
                    Term.I(EtsHeir419HostNames.Key)
                )).Equals(Term.List(Term.Tuple(Term.I(EtsHeir419HostNames.Key), Term.I(EtsHeir419HostNames.Value)))));
                _ = await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Setopts,
                    table,
                    Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsHeir419HostNames.None))
                );
                _ = await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Delete,
                    table
                );

                return true;
            });
            await owner.Process.Completion.WaitAsync(EtsHeir419HostNames.Deadline);
        }
    }

    internal static async Task AtomicQueue()
    {
        await using var runtime = new ProcessRuntime(2);
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsHeir419HostNames.Runtime,
            EtsHeir419HostNames.ProcessFlag,
            Term.A(EtsHeir419HostNames.Scheduler),
            Term.I(EtsHeir419HostNames.OwnerLane)
        ));
        ProcessContext target = await receiver.Ready;
        _ = await receiver.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsHeir419HostNames.Runtime,
            EtsHeir419HostNames.ProcessFlag,
            Term.A(EtsHeir419HostNames.Scheduler),
            Term.I(EtsHeir419HostNames.ReceiverLane)
        ));
        Term table = await owner.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.New,
            Term.A(EtsHeir419HostNames.Table),
            Term.List(Term.A(EtsHeir419HostNames.Named), Term.A(EtsHeir419HostNames.Public))
        ));
        object gate = typeof(ProcessRuntime).GetField(EtsHeir419HostNames.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        PropertyInfo incoming = typeof(Mailbox).GetProperty(EtsHeir419HostNames.Incoming, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (Action<Term>?)incoming.GetValue(target.Mailbox);
        int observations = 0;
        incoming.SetValue(
            target.Mailbox,
            (Action<Term>)(value =>
        {
            original?.Invoke(value);
            if (!Tag(value, EtsHeir419HostNames.Transfer)) return;
            Require(!Monitor.IsEntered(gate));
            // Real native owner check while IncomingTerm is held proves ownership is not yet exposed.
            Task<bool> ownershipCheck;
            using (ExecutionContext.SuppressFlow())
                ownershipCheck = Task.Run(() => receiver.InvokeAsync(async current =>
            {
                try
                {
                    _ = await Call(
                        runtime,
                        current,
                        EtsHeir419HostNames.Setopts,
                        table,
                        Nil.Value
                    );

                    return false;
                }
                catch (ErlangException error)
                {
                    return error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
            }));
            bool oldOwner = ownershipCheck.GetAwaiter().GetResult();
            Require(oldOwner);
            Interlocked.Increment(ref observations);
        })
        );
        try
        {
            _ = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Give,
                table,
                receiver.Process.Pid,
                Gift()
            ));
            Require(observations == 1);
            await receiver.InvokeAsync(async c =>
            {
                _ = await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Setopts,
                    table,
                    Nil.Value
                );
                Require(await Receive(c, EtsHeir419HostNames.Transfer, TimeSpan.Zero) is TupleTerm { Items.Count: 4 });
                _ = await Call(
                    runtime,
                    c,
                    EtsHeir419HostNames.Delete,
                    table
                );

                return true;
            });
        }
        finally
        {
            incoming.SetValue(target.Mailbox, original);
        }
    }

    internal static async Task DeadHeir()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var doomed = new RuntimeNodeFoundationHostActor(runtime);
        await using var survivor = new RuntimeNodeFoundationHostActor(runtime);
        Term old = Term.A(EtsHeir419HostNames.Table), next = Term.A(EtsHeir419HostNames.Next);
        _ = await owner.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.New,
            next,
            Term.List(Term.A(EtsHeir419HostNames.Named))
        ));
        _ = await owner.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.Setopts,
            next,
            Term.Tuple(Term.A(EtsHeir419HostNames.Heir), doomed.Process.Pid, Gift())
        ));
        _ = await survivor.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.New,
            old,
            Term.List(Term.A(EtsHeir419HostNames.Named))
        ));
        Term reference = await survivor.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.Whereis,
            old
        ));
        object gate = typeof(ProcessRuntime).GetField(EtsHeir419HostNames.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        await survivor.Invoke(c =>
        {
            lock (gate)
            {
                runtime.Exit(c, doomed.Process.Pid, Term.A(EtsHeir419HostNames.Kill));
                runtime.Exit(c, owner.Process.Pid, Term.A(EtsHeir419HostNames.Kill));
                var rename = runtime.Modules.Call(
                    c,
                    EtsHeir419HostNames.Ets,
                    EtsHeir419HostNames.Rename,
                    reference,
                    next
                );
                Require(rename.IsCompletedSuccessfully && rename.GetAwaiter().GetResult().Equals(next));
            }

            return true;
        });
        await Task.WhenAll(owner.Process.Completion, doomed.Process.Completion).WaitAsync(EtsHeir419HostNames.Deadline);
        Require((await survivor.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.Whereis,
            next
        ))).Equals(reference));
        _ = await survivor.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.Delete,
            reference
        ));
    }

    internal static async Task Options()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var borrower = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async c => await Call(
            runtime,
            c,
            EtsHeir419HostNames.New,
            Term.A(EtsHeir419HostNames.Table),
            Term.List(Term.A(EtsHeir419HostNames.Named), Term.A(EtsHeir419HostNames.Public))
        ));
        Term privateOption = Term.Tuple(Term.A(EtsHeir419HostNames.Protection), Term.A(EtsHeir419HostNames.Private));
        try
        {
            await BadArgument(() => owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Setopts,
                table,
                Term.List(privateOption, privateOption)
            )));
            _ = await borrower.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Insert,
                table,
                Term.Tuple(Term.I(EtsHeir419HostNames.Key))
            ));
            await BadArgument(() => borrower.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Setopts,
                table,
                Nil.Value
            )));
            _ = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Setopts,
                table,
                privateOption
            ));
            await BadArgument(() => borrower.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Lookup,
                table,
                Term.I(EtsHeir419HostNames.Key)
            )));
            _ = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Setopts,
                table,
                Term.Tuple(Term.A(EtsHeir419HostNames.Heir), borrower.Process.Pid, Gift())
            ));
            _ = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Setopts,
                table,
                Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsHeir419HostNames.None))
            ));
        }
        finally
        {
            _ = await owner.InvokeAsync(async c => await Call(
                runtime,
                c,
                EtsHeir419HostNames.Delete,
                table
            ));
        }
    }
}

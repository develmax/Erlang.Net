using Erlang;
namespace Erlang.Tests;

internal static class EtsNewHeir419Checks
{
    private static void Register(ProcessRuntime r)
    {
        RuntimeNativePrimitiveProviders.Register(r, r.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsHeir419.Register(r, r.Modules);
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

    internal static async Task Lifetime()
    {
        foreach (int mode in new[] { 0, 1, 2 })
        {
            await using var r = new ProcessRuntime();
            Register(r);
            object storage = EtsFoundation405Access.Get(r);
            await using var owner = new RuntimeNodeFoundationHostActor(r);
            await using var heir = new RuntimeNodeFoundationHostActor(r);
            Term first = Term.Tuple(Term.A(EtsNewHeir419Names.Data), Term.I(EtsHeir419HostNames.Value));
            Term last = Term.Tuple(Term.A(EtsNewHeir419Names.Last), Term.List(Term.I(EtsHeir419HostNames.Value)));
            Term table = await owner.InvokeAsync(async c =>
            {
                Term tail = mode == 0 ? Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid, last) : mode == 1 ? Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid) : Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsHeir419HostNames.None));
                Term id = await Call(
                    r,
                    c,
                    EtsHeir419HostNames.New,
                    Term.A(EtsNewHeir419Names.Table),
                    Term.List(
                        Term.A(EtsHeir419HostNames.Named),
                        Term.A(EtsHeir419HostNames.Private),
                        Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid, first),
                        tail
                    )
                );
                _ = await Call(
                    r,
                    c,
                    EtsHeir419HostNames.Insert,
                    id,
                    Term.Tuple(Term.I(EtsHeir419HostNames.Key), Term.I(EtsHeir419HostNames.Value))
                );

                return id;
            });
            ReferenceTerm monitor = await heir.Invoke(c => r.Monitor(c, owner.Process.Pid));
            await heir.InvokeAsync(async c =>
            {
                r.Exit(c, owner.Process.Pid, Term.A(EtsHeir419HostNames.Kill));
                Term? observed = await c.Mailbox.ReceiveAsync<Term>(
                    v => v is TupleTerm t && t.Items.Count > 0 && (t.Items[0].Equals(Term.A(EtsHeir419HostNames.Transfer)) || t.Items[0].Equals(Term.A(EtsHeir419HostNames.Down))) ? v : null,
                    EtsHeir419HostNames.Deadline,
                    c.Cancellation
                );
                if (mode == 0)
                {
                    EtsFoundation405Access.Require(observed is TupleTerm { Items.Count: 4 } transfer && transfer.Items[1].Equals(table) && transfer.Items[2].Equals(owner.Process.Pid) && transfer.Items[3].Equals(last) && !ReferenceEquals(transfer.Items[3], last));
                    observed = await c.Mailbox.ReceiveAsync<Term>(
                        v => v is TupleTerm { Items.Count: 5 } t && t.Items[0].Equals(Term.A(EtsHeir419HostNames.Down)) ? v : null,
                        EtsHeir419HostNames.Deadline,
                        c.Cancellation
                    );
                }
                EtsFoundation405Access.Require(observed is TupleTerm { Items.Count: 5 } down && down.Items[1].Equals(monitor));
                EtsFoundation405Access.Require(await c.Mailbox.ReceiveAsync<Term>(
                    v => v is TupleTerm { Items.Count: 4 } t && t.Items[0].Equals(Term.A(EtsHeir419HostNames.Transfer)) ? v : null,
                    TimeSpan.Zero,
                    c.Cancellation
                ) is null);
                if (mode == 2) EtsFoundation405Access.Require((await Call(
                    r,
                    c,
                    EtsHeir419HostNames.Whereis,
                    table
                )).Equals(Term.A(EtsNewHeir419Names.Undefined)));
                else
                {
                    EtsFoundation405Access.Require((await Call(
                        r,
                        c,
                        EtsHeir419HostNames.Lookup,
                        table,
                        Term.I(EtsHeir419HostNames.Key)
                    )).Equals(Term.List(Term.Tuple(Term.I(EtsHeir419HostNames.Key), Term.I(EtsHeir419HostNames.Value)))));
                    _ = await Call(
                        r,
                        c,
                        EtsHeir419HostNames.Delete,
                        table
                    );
                }

                return true;
            });
            await owner.Process.Completion.WaitAsync(EtsHeir419HostNames.Deadline);
            EtsFoundation405Access.Empty(storage);
        }
    }

    internal static async Task Rollback()
    {
        await using var r = new ProcessRuntime();
        Register(r);
        object storage = EtsFoundation405Access.Get(r);
        await using var owner = new RuntimeNodeFoundationHostActor(r);
        await using var heir = new RuntimeNodeFoundationHostActor(r);
        await owner.InvokeAsync(async c =>
        {
            Term name = Term.A(EtsNewHeir419Names.Table);
            Term named = Term.A(EtsHeir419HostNames.Named);
            Term table = await Call(
                r,
                c,
                EtsHeir419HostNames.New,
                name,
                Term.List(named)
            );
            var before = EtsFoundation405Access.Snapshot(storage);
            try
            {
                Term gift = Term.Tuple(Term.A(EtsNewHeir419Names.Data), Term.List(Term.I(EtsHeir419HostNames.Value)));
                bool failed = false;
                try
                {
                    _ = await Call(
                        r,
                        c,
                        EtsHeir419HostNames.New,
                        name,
                        Term.List(named, Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid, gift))
                    );
                }
                catch (ErlangException e)
                {
                    failed = e.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                EtsFoundation405Access.Require(failed && before.Equals(EtsFoundation405Access.Snapshot(storage)));
                // Invalid earlier heir must not be ignored when a later valid none would win.
                failed = false;
                try
                {
                    _ = await Call(
                        r,
                        c,
                        EtsHeir419HostNames.New,
                        name,
                        Term.List(
                            Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsNewHeir419Names.BadHeir)),
                            Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsHeir419HostNames.None))
                        )
                    );
                }
                catch (ErlangException e)
                {
                    failed = e.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                EtsFoundation405Access.Require(failed && before.Equals(EtsFoundation405Access.Snapshot(storage)));
            }
            finally
            {
                _ = await Call(
                    r,
                    c,
                    EtsHeir419HostNames.Delete,
                    table
                );
            }

            return true;
        });
        EtsFoundation405Access.Empty(storage);
    }
}

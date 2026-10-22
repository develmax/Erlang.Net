namespace Erlang.Tests;

internal static class PersistentTerm408HostChecks
{
    internal static async Task ExactValues()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = PersistentTerm408HostAccess.Store(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(async context =>
            {
                Term key = Term.A(PersistentTerm408HostNames.Key);
                Term shared = Term.Tuple(Term.A(PersistentTerm408HostNames.Shared), new BitString(new byte[] { 4, 0, 8 }));
                var value = Term.Tuple(shared, shared);
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    value
                );
                var restored = (TupleTerm)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    key
                )!;
                PersistentTerm408HostAccess.Require(restored.Equals(value) && !ReferenceEquals(restored, value)
                    && ReferenceEquals(restored.Items[0], restored.Items[1]));
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    value,
                    true
                );
                bool conflict = false;
                try
                {
                    await PersistentTerm408HostAccess.Put(
                        store,
                        context,
                        key,
                        Term.I(9),
                        true
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    conflict = true;
                }
                PersistentTerm408HostAccess.Require(conflict);
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    Term.I(1),
                    Term.I(11)
                );
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    new FloatTerm(1),
                    Term.I(22)
                );
                PersistentTerm408HostAccess.Require(((Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    Term.I(1)
                )!).Equals(Term.I(11)));
                PersistentTerm408HostAccess.Require(((Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    new FloatTerm(1)
                )!).Equals(Term.I(22)));
                Term fallback = Term.Tuple(Term.I(44));
                PersistentTerm408HostAccess.Require(ReferenceEquals(fallback, PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    Term.I(404),
                    fallback
                )));
                await ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Erase,
                    context,
                    key
                )!);
                PersistentTerm408HostAccess.Require(restored.Equals(value));

                return true;
            });
        }
        finally
        {
            await (Task)PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Close)!;
        }
    }

    internal static async Task ProcessIndependentAndAtomicNew()
    {
        await using var runtime = new ProcessRuntime();
        object store = PersistentTerm408HostAccess.Store(runtime);
        var writer = new RuntimeNodeFoundationHostActor(runtime);
        var readers = new[] { new RuntimeNodeFoundationHostActor(runtime), new RuntimeNodeFoundationHostActor(runtime) };
        try
        {
            await writer.InvokeAsync(context => PersistentTerm408HostAccess.Put(
                store,
                context,
                Term.I(1),
                Term.Tuple(Term.I(408))
            ));
            await writer.DisposeAsync();
            await readers[0].Invoke(context =>
            {
                PersistentTerm408HostAccess.Require(((Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    Term.I(1)
                )!).Equals(Term.Tuple(Term.I(408))));

                return true;
            });
            await Task.WhenAll(readers.Select(actor => actor.InvokeAsync(context =>
                PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    Term.I(2),
                    Term.Tuple(Term.I(1)),
                    true
                ))));
            await readers[1].Invoke(context =>
            {
                PersistentTerm408HostAccess.Require(((Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    Term.I(2)
                )!).Equals(Term.Tuple(Term.I(1))));

                return true;
            });
        }
        finally
        {
            var failures = new List<Exception>();
            try
            {
                await writer.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            foreach (var actor in readers)
            {
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            try
            {
                await (Task)PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Close)!;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
    }

    internal static async Task RetiredReaderAndClose()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = PersistentTerm408HostAccess.Store(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object? held = null;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    Term.I(1),
                    Term.Tuple(Term.I(408))
                );
                held = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Snapshot)!;
                await ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Erase,
                    context,
                    Term.I(1)
                )!);

                return true;
            });
            await (Task)PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Close)!;
            object before = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
            PersistentTerm408HostAccess.Require((long)before.GetType().GetProperty(PersistentTerm408HostNames.Payloads)!.GetValue(before)! == 1);
            var materialized = (TupleTerm?[])PersistentTerm408HostAccess.Call(held!, PersistentTerm408HostNames.Materialize, CancellationToken.None)!;
            PersistentTerm408HostAccess.Require(materialized.OfType<TupleTerm>().Single().Items[1].Equals(Term.Tuple(Term.I(408))));
            ((IDisposable)held!).Dispose();
            held = null;
            object after = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
            PersistentTerm408HostAccess.Require((long)after.GetType().GetProperty(PersistentTerm408HostNames.Payloads)!.GetValue(after)! == 0);
        }
        finally
        {
            ((IDisposable?)held)?.Dispose();
            await (Task)PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Close)!;
        }
    }
}

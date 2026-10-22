namespace Erlang.Tests;

internal static class PersistentLifecycle408Checks
{
    internal static async Task Whole()
    {
        await Scenario(false, false, false);
        await Scenario(true, false, false);
        await Scenario(false, true, false);
        await Scenario(false, false, true);
    }

    private static ProcessContext Initializer(ProcessRuntime runtime)
    {
        object gate = typeof(ProcessRuntime).GetField(PersistentLifecycle408Names.Gate, PersistentLifecycle408Names.Members)!.GetValue(runtime)!;
        lock (gate)
        {
            var processes = (Dictionary<Pid, ProcessContext>)typeof(ProcessRuntime)
                .GetField(PersistentLifecycle408Names.Processes, PersistentLifecycle408Names.Members)!.GetValue(runtime)!;

            return processes[runtime.InitializationPid];
        }
    }

    private static long Payloads(object store)
    {
        object report = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;

        return (long)report.GetType().GetProperty(PersistentTerm408HostNames.Payloads)!.GetValue(report)!;
    }

    private static void ClosedAndEmpty(object store, long retained)
    {
        object report = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
        PersistentTerm408HostAccess.Require((bool)report.GetType().GetProperty(PersistentLifecycle408Names.Closed)!.GetValue(report)!);
        PersistentTerm408HostAccess.Require((int)report.GetType().GetProperty(PersistentLifecycle408Names.ActivePairs)!.GetValue(report)! == 0);
        PersistentTerm408HostAccess.Require(Payloads(store) == retained);
        PersistentTerm408HostAccess.Require(PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedTables) == 0
            && PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedPairBytes) == 0
            && PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedTableBytes) == 0);
    }

    internal static async Task Scenario(bool halt, bool initializerEnded, bool retainOne)
    {
        var runtime = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        object? held = null;
        var failures = new List<Exception>();
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            ProcessContext initializer = Initializer(runtime);
            object store = PersistentTerm408HostAccess.Store(runtime);
            actor = new(runtime);
            await actor.InvokeAsync(async context =>
            {
                for (int index = 0; index < PersistentLifecycle408Names.Pairs; index++)
                {
                    await PersistentTerm408HostAccess.Put(
                        store,
                        context,
                        Term.I(index),
                        Term.Tuple(Term.I(index))
                    );
                    if (retainOne && index == 0)
                        held = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Snapshot)!;
                }
                PersistentTerm408HostAccess.Require(Payloads(store) == PersistentLifecycle408Names.Pairs);

                return true;
            });
            Task shutdown;
            if (halt)
            {
                Task selected = runtime.RequestHalt(0, new RuntimeHaltOptions(false));
                shutdown = Task.WhenAll(selected, runtime.HaltQuiescence);
            }
            else if (initializerEnded)
            {
                await actor.Invoke(context =>
                {
                    runtime.Exit(context, runtime.InitializationPid, Term.A(PersistentLifecycle408Names.Kill));

                    return true;
                });
                var done = (TaskCompletionSource<Term>)typeof(ProcessContext)
                    .GetProperty(PersistentLifecycle408Names.Done, PersistentLifecycle408Names.Members)!.GetValue(initializer)!;
                var retirement = (Task)store.GetType().GetField(PersistentLifecycle408Names.Retirement,
                    PersistentLifecycle408Names.Members)!.GetValue(store)!;
                shutdown = Task.WhenAll(done.Task, actor.Process.Completion, retirement);
            }
            else shutdown = runtime.DisposeAsync().AsTask();
            if (retainOne)
            {
                // Observe the actual lifecycle-installed retirement. Calling Close
                // here would mask a missing lifecycle hook and is forbidden.
                await PersistentNative417Checks.WaitRetained(store, 1);
                PersistentTerm408HostAccess.Require(!shutdown.IsCompleted);
                var copied = (TupleTerm?[])PersistentTerm408HostAccess.Call(held!, PersistentTerm408HostNames.Materialize, CancellationToken.None)!;
                var pair = copied.OfType<TupleTerm>().Single();
                PersistentTerm408HostAccess.Require(pair.Items[0].Equals(Term.I(0)) && pair.Items[1].Equals(Term.Tuple(Term.I(0))));
                ((IDisposable)held!).Dispose();
                held = null;
            }
            await shutdown.WaitAsync(PersistentLifecycle408Names.Deadline);
            ClosedAndEmpty(store, 0);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            try
            {
                ((IDisposable?)held)?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}

using System.Collections;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsHeir419LeaseChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run()
    {
        foreach (bool clear in new[] { true, false })
        {
            var runtime = new ProcessRuntime();
            object storage = EtsFoundation405Access.Get(runtime);
            RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
            RuntimeBootstrapEtsHeir419.Register(runtime, runtime.Modules);
            var owner = new RuntimeNodeFoundationHostActor(runtime);
            var heir = new RuntimeNodeFoundationHostActor(runtime);
            IDisposable? lease = null;
            try
            {
                Term gift = Term.Tuple(Term.A(EtsHeir419HostNames.Data), Term.List(Term.I(EtsHeir419HostNames.Value)));
                Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                    c,
                    EtsHeir419HostNames.Ets,
                    EtsHeir419HostNames.New,
                    new Term[] { Term.A(EtsHeir419HostNames.Table), Nil.Value }
                ));
                _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                    c,
                    EtsHeir419HostNames.Ets,
                    EtsHeir419HostNames.Setopts,
                    new Term[] { table, Term.Tuple(Term.A(EtsHeir419HostNames.Heir), heir.Process.Pid, gift) }
                ));
                object gate = typeof(ProcessRuntime).GetField(EtsHeir419HostNames.Gate, Members)!.GetValue(runtime)!;
                lock (gate)
                {
                    var tables = (IDictionary)storage.GetType().GetField(EtsHeir419HostNames.TablesField, Members)!.GetValue(storage)!;
                    EtsFoundation405Access.Require(tables.Count == 1);
                    foreach (object actual in tables.Values)
                    {
                        object policy = actual.GetType().GetProperty(EtsHeir419HostNames.HeirProperty, Members)!.GetValue(actual)!;
                        object data = policy.GetType().GetProperty(EtsHeir419HostNames.DataProperty, Members)!.GetValue(policy)!;
                        lease = (IDisposable)EtsFoundation405Access.Invoke(data, EtsHeir419HostNames.AcquireMethod)!;
                    }
                }
                if (clear)
                    _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                        c,
                        EtsHeir419HostNames.Ets,
                        EtsHeir419HostNames.Setopts,
                        new Term[] { table, Term.Tuple(Term.A(EtsHeir419HostNames.Heir), Term.A(EtsHeir419HostNames.None)) }
                    ));
                // Actual logical Close releases table ownership but cannot revoke an admitted read lease.
                await runtime.DisposeAsync();
                EtsFoundation405Access.Require((long)EtsFoundation405Access.Snapshot(storage)[3]! == 1);
                Term materialized = (Term)EtsFoundation405Access.Invoke(lease!, EtsFoundation405Names.Materialize, CancellationToken.None)!;
                EtsFoundation405Access.Require(materialized.Equals(Term.Tuple(gift)) && !ReferenceEquals(((TupleTerm)materialized).Items[0], gift));
                lease!.Dispose();
                lease = null;
                EtsFoundation405Access.Empty(storage, true);
            }
            finally
            {
                List<Exception> failures = new();
                try
                {
                    lease?.Dispose();
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
                try
                {
                    await owner.DisposeAsync();
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
                try
                {
                    await heir.DisposeAsync();
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
                if (failures.Count != 0) throw new AggregateException(failures);
            }
        }
    }
}

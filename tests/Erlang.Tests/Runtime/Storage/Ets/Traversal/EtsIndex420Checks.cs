using System.Collections;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsIndex420Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsIndex420Names.Failure);
    }

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsDeletion.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsMutations414.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsCounter417.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsTraversal416.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string name,
        params Term[] arguments
    ) => runtime.Modules.Call(
        context,
        EtsIndex420Names.Module,
        name,
        arguments
    );

    private static Task<Term> Create(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, string kind) => actor.InvokeAsync(async context => await Call(
        runtime,
        context,
        EtsIndex420Names.New,
        Term.A(EtsIndex420Names.Name),
        Term.List(Term.A(EtsIndex420Names.Public), Term.A(kind))
    ));

    private static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static int Extent(object storage, Term table) => (int)Property(
        Property(EtsFoundation405Access.Invoke(storage, EtsIndex420Names.Resolve, table)!, EtsIndex420Names.Index),
        EtsIndex420Names.BucketCount
    );

    private static async Task Drain(object storage) => await (Task)EtsFoundation405Access.Invoke(storage, EtsIndex420Names.Drain)!;

    private static Term Rows(int start, int count) => Cons.From(Enumerable.Range(start, count).Select(key => Term.Tuple(Term.I(key), Term.I(key))));

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(1);
        Require(!runtime.Modules.HasExport(EtsIndex420Names.Module, EtsIndex420Names.Fix, 2));
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        Require(runtime.Modules.HasExport(EtsIndex420Names.Module, EtsIndex420Names.Slot, 2) && runtime.Modules.SourceOwner(EtsIndex420Names.Module, EtsIndex420Names.Slot, 2) is null);
        await using var foreign = new ProcessRuntime(1);
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsFixation420.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && !foreign.Modules.HasExport(EtsIndex420Names.Module, EtsIndex420Names.Fix, 2));
        await using var rollback = new ProcessRuntime(1);
        var expected = new InvalidOperationException(EtsIndex420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsFixation420.Register(rollback, registry);
                throw expected;
            });
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, expected)) { }
        Require(!rollback.Modules.HasExport(EtsIndex420Names.Module, EtsIndex420Names.Fix, 2) && !rollback.Modules.HasExport(EtsIndex420Names.Module, EtsIndex420Names.Slot, 2));
    }

    internal static async Task Buckets()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsIndex420Names.Set);
        await actor.InvokeAsync(async context =>
        {
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Insert,
                table,
                Rows(0, EtsIndex420Names.Rows)
            );
            int first = Extent(storage, table);
            Require(first >= EtsIndex420Names.Rows);
            var actual = new HashSet<Term>();
            for (int slot = 0; slot < first; slot++) foreach (var row in Cons.Items(await Call(
                runtime,
                context,
                EtsIndex420Names.Slot,
                table,
                Term.I(slot)
            ))) Require(actual.Add(row));
            Require(actual.SetEquals(Cons.Items(Rows(0, EtsIndex420Names.Rows))));
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Slot,
                table,
                Term.I(first)
            )).Equals(Term.A(EtsIndex420Names.End)));
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.True)
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Insert,
                table,
                Rows(EtsIndex420Names.Rows, EtsIndex420Names.More)
            );
            Require(Extent(storage, table) == first);
            for (int key = 0; key < EtsIndex420Names.Rows + EtsIndex420Names.More; key++)
            {
                Require((await Call(
                    runtime,
                    context,
                    EtsIndex420Names.Lookup,
                    table,
                    Term.I(key)
                )).Equals(Term.List(Term.Tuple(Term.I(key), Term.I(key)))));
            }
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.False)
            );
            Require(Extent(storage, table) >= EtsIndex420Names.Rows + EtsIndex420Names.More);
            bool raw = false;
            try
            {
                _ = EtsFoundation405Access.Invoke(
                    storage,
                    EtsIndex420Names.Store,
                    context,
                    table,
                    Term.Tuple(Term.I(-1))
                );
            }
            catch (InvalidOperationException)
            {
                raw = true;
            }
            Require(raw);

            return true;
        });
    }

    internal static async Task Fixed()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsIndex420Names.Bag);
        await actor.InvokeAsync(async context =>
        {
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Insert,
                table,
                Term.List(Term.Tuple(Term.I(42), Term.A(EtsIndex420Names.First)), Term.Tuple(Term.I(42), Term.A(EtsIndex420Names.Second)))
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.True)
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.True)
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Delete,
                table,
                Term.I(42)
            );
            Require((int)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.LiveObjects]! == 0);
            Require((long)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.Payloads]! == 1);
            Require(await Call(
                runtime,
                context,
                EtsIndex420Names.Lookup,
                table,
                Term.I(42)
            ) is Nil);
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Next,
                table,
                Term.I(42)
            )).Equals(Term.A(EtsIndex420Names.End)));
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.False)
            );
            Require((long)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.Payloads]! == 1);
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.False)
            );
            Require((long)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.Payloads]! == 0);
            bool refused = false;
            try
            {
                _ = await Call(
                    runtime,
                    context,
                    EtsIndex420Names.Next,
                    table,
                    Term.I(42)
                );
            }
            catch (ErlangException)
            {
                refused = true;
            }
            Require(refused);

            return true;
        });
    }

    internal static async Task Death()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var fixer = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsIndex420Names.Set);
        try
        {
            _ = await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsIndex420Names.Insert,
                table,
                Term.Tuple(Term.I(42), Term.I(1))
            ));
            _ = await fixer.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.True)
            ));
            _ = await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsIndex420Names.Delete,
                table,
                Term.I(42)
            ));
            Require((long)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.Payloads]! == 1);
            await fixer.DisposeAsync();
            Require((long)EtsFoundation405Access.Snapshot(storage)[EtsIndex420Names.Payloads]! == 0);
            await using var second = new RuntimeNodeFoundationHostActor(runtime);
            _ = await second.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsIndex420Names.Fix,
                table,
                Term.A(EtsIndex420Names.True)
            ));
            _ = await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsIndex420Names.Delete,
                table
            ));
            await Drain(storage);
            var fixing = (IDictionary)storage.GetType().GetField(EtsIndex420Names.Fixers, Members)!.GetValue(storage)!;
            Require(fixing.Count == 0);
            EtsFoundation405Access.Empty(storage);
        }
        finally
        {
            await fixer.DisposeAsync();
        }
    }

    internal static async Task Mutations()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await Create(runtime, actor, EtsIndex420Names.Ordered);
        await actor.InvokeAsync(async context =>
        {
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Insert,
                table,
                Term.List(Term.Tuple(Term.I(3), Term.I(30)), Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)))
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Counter,
                table,
                Term.I(2),
                Term.I(5)
            );
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.Update,
                table,
                Term.I(3),
                Term.Tuple(Term.I(2), Term.I(35))
            );
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Slot,
                table,
                Term.I(1)
            )).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(25)))));
            _ = await Call(
                runtime,
                context,
                EtsIndex420Names.DeleteObject,
                table,
                Term.Tuple(Term.I(1), Term.I(10))
            );
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Take,
                table,
                Term.I(2)
            )).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(25)))));
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Slot,
                table,
                Term.I(0)
            )).Equals(Term.List(Term.Tuple(Term.I(3), Term.I(35)))));
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.AllDelete,
                table,
                Term.A(EtsIndex420Names.Undefined)
            )).Equals(Term.I(1)));
            Require((await Call(
                runtime,
                context,
                EtsIndex420Names.Slot,
                table,
                Term.I(0)
            )).Equals(Term.A(EtsIndex420Names.End)));

            return true;
        });
    }
}

using Erlang;
namespace Erlang.Tests;

internal static class EtsTraversal416HostChecks
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsTraversal416HostNames.Failure);
    }

    internal static void Register(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsTraversal416.Register(runtime, runtime.Modules);
    }

    internal static async Task Ordered()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        RuntimeBootstrapEtsTraversal416.Register(runtime, runtime.Modules);
        Require(runtime.NativePrimitives.Bindings.Count == 11);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            async Task<Term> Call(string f, params Term[] args) => await runtime.Modules.Call(
                c,
                EtsTraversal416HostNames.Ets,
                f,
                args
            );
            Term table = await Call(
                EtsTraversal416HostNames.New,
                Term.A(EtsTraversal416HostNames.Table),
                Term.List(Term.A(EtsTraversal416HostNames.Ordered))
            );
            Term low = new FloatTerm(1.0), middle = Term.I(2), high = Term.I(3);
            Term row1 = Term.Tuple(low, Term.I(10)), row2 = Term.Tuple(middle, Term.I(20)), row3 = Term.Tuple(high, Term.I(30));
            try
            {
                _ = await Call(EtsTraversal416HostNames.Insert, table, Term.List(row3, row1, row2));
                Require((await Call(EtsTraversal416HostNames.First, table)).Equals(low));
                Require((await Call(EtsTraversal416HostNames.Last, table)).Equals(high));
                Require((await Call(EtsTraversal416HostNames.Next, table, Term.I(1))).Equals(middle));
                Require((await Call(EtsTraversal416HostNames.Previous, table, new FloatTerm(2.0))).Equals(low));
                Require((await Call(EtsTraversal416HostNames.FirstLookup, table)).Equals(Term.Tuple(low, Term.List(row1))));
                Require((await Call(EtsTraversal416HostNames.LastLookup, table)).Equals(Term.Tuple(high, Term.List(row3))));
                Require((await Call(EtsTraversal416HostNames.NextLookup, table, new FloatTerm(1.5))).Equals(Term.Tuple(middle, Term.List(row2))));
                Require((await Call(EtsTraversal416HostNames.PreviousLookup, table, new FloatTerm(2.5))).Equals(Term.Tuple(middle, Term.List(row2))));
                Require((await Call(EtsTraversal416HostNames.Next, table, high)).Equals(Term.A(EtsTraversal416HostNames.End)));
                foreach (string f in new[] { EtsTraversal416HostNames.Next, EtsTraversal416HostNames.Previous, EtsTraversal416HostNames.NextLookup, EtsTraversal416HostNames.PreviousLookup })
                    await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                        c,
                        EtsTraversal416HostNames.Ets,
                        f,
                        table,
                        Term.A(EtsTraversal416HostNames.End)
                    ));
            }
            finally
            {
                _ = await Call(EtsTraversal416HostNames.Delete, table);
            }
            Term empty = await Call(
                EtsTraversal416HostNames.New,
                Term.A(EtsTraversal416HostNames.Table),
                Term.List(Term.A(EtsTraversal416HostNames.Ordered))
            );
            try
            {
                foreach (var signature in EtsTraversal416HostNames.Signatures)
                {
                    Term value = signature.Arity == 1 ? await Call(signature.Function, empty) : await Call(signature.Function, empty, middle);
                    Require(value.Equals(Term.A(EtsTraversal416HostNames.End)));
                }
            }
            finally
            {
                _ = await Call(EtsTraversal416HostNames.Delete, empty);
            }

            return true;
        });
    }

    internal static async Task HashGroups()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            async Task<Term> Call(string f, params Term[] args) => await runtime.Modules.Call(
                c,
                EtsTraversal416HostNames.Ets,
                f,
                args
            );
            foreach (string kind in new[] { EtsTraversal416HostNames.Set, EtsTraversal416HostNames.Bag, EtsTraversal416HostNames.Duplicate })
            {
                Term table = await Call(EtsTraversal416HostNames.New, Term.A(EtsTraversal416HostNames.Table), Term.List(Term.A(kind)));
                try
                {
                    _ = await Call(
                        EtsTraversal416HostNames.Insert,
                        table,
                        Term.List(
                            Term.Tuple(Term.I(2), Term.I(20)),
                            Term.Tuple(Term.I(1), Term.I(10)),
                            Term.Tuple(Term.I(2), Term.I(21)),
                            Term.Tuple(Term.I(2), Term.I(20))
                        )
                    );
                    Term first = await Call(EtsTraversal416HostNames.First, table);
                    Require(first.Equals(await Call(EtsTraversal416HostNames.Last, table)));
                    Require((await Call(EtsTraversal416HostNames.FirstLookup, table)).Equals(await Call(EtsTraversal416HostNames.LastLookup, table)));
                    var seen = new HashSet<Term>();
                    Term key = first;
                    while (!key.Equals(Term.A(EtsTraversal416HostNames.End)))
                    {
                        Require(seen.Add(key) && seen.Count <= 2);
                        Term next = await Call(EtsTraversal416HostNames.Next, table, key);
                        Require(next.Equals(await Call(EtsTraversal416HostNames.Previous, table, key)));
                        Term lookup = await Call(EtsTraversal416HostNames.NextLookup, table, key);
                        Require(lookup.Equals(await Call(EtsTraversal416HostNames.PreviousLookup, table, key)));
                        if (!next.Equals(Term.A(EtsTraversal416HostNames.End))) Require(lookup.Equals(Term.Tuple(next, await Call(EtsTraversal416HostNames.Lookup, table, next))));
                        else Require(lookup.Equals(next));
                        key = next;
                    }
                    Require(seen.SetEquals(new Term[] { Term.I(1), Term.I(2) }));
                    await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                        c,
                        EtsTraversal416HostNames.Ets,
                        EtsTraversal416HostNames.Next,
                        table,
                        Term.I(99)
                    ));
                }
                finally
                {
                    _ = await Call(EtsTraversal416HostNames.Delete, table);
                }
            }

            return true;
        });
    }

    internal static async Task RightsAndDetached()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsTraversal416HostNames.Ets,
            EtsTraversal416HostNames.New,
            Term.A(EtsTraversal416HostNames.Table),
            Term.List(Term.A(EtsTraversal416HostNames.Private))
        ));
        Term row = Term.Tuple(Term.I(1), Term.List(Term.I(10), Term.I(11)));
        _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsTraversal416HostNames.Ets,
            EtsTraversal416HostNames.Insert,
            table,
            row
        ));
        await other.InvokeAsync(async c =>
        {
            foreach (var signature in EtsTraversal416HostNames.Signatures)
                await EtsSelect409Access.BadArgument(() => signature.Arity == 1 ? runtime.Modules.Call(
                    c,
                    EtsTraversal416HostNames.Ets,
                    signature.Function,
                    table
                )
           : runtime.Modules.Call(
               c,
               EtsTraversal416HostNames.Ets,
               signature.Function,
               table,
               Term.I(1)
           ));

            return true;
        });
        Term detached = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsTraversal416HostNames.Ets,
            EtsTraversal416HostNames.FirstLookup,
            table
        ));
        _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsTraversal416HostNames.Ets,
            EtsTraversal416HostNames.Delete,
            table
        ));
        Require(detached.Equals(Term.Tuple(Term.I(1), Term.List(row))));
        EtsFoundation405Access.Empty(EtsFoundation405Access.Get(runtime));
    }

    internal static async Task Registration()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        await using var foreign = new ProcessRuntime();
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsTraversal416.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        runtime.Modules.Register(
            EtsTraversal416HostNames.Ets,
            EtsTraversal416HostNames.PreviousLookup,
            2,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        refused = false;
        try
        {
            RuntimeBootstrapEtsTraversal416.Register(runtime, runtime.Modules);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused && runtime.NativePrimitives.Bindings.Count == 11);
        foreach (var signature in EtsTraversal416HostNames.Signatures)
            Require(runtime.Modules.HasExport(EtsTraversal416HostNames.Ets, signature.Function, signature.Arity) == (signature.Function == EtsTraversal416HostNames.PreviousLookup));
    }
}

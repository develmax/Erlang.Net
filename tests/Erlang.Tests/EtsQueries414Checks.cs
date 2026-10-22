using Erlang;
namespace Erlang.Tests;

internal static class EtsQueries414Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsQueries414Names.Failure);
    }

    private static Term Specification(Term body) => Term.List(Term.Tuple(
        Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.List(body)));

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        Require(!runtime.Modules.HasExport(EtsQueries414Names.Ets, EtsQueries414Names.Count, 2));
        RuntimeBootstrapEtsQueries414.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsQueries414.Register(runtime, runtime.Modules);
        Require(runtime.NativePrimitives.Bindings.Count == 11);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async Task<Term> Call(string function, params Term[] args) => await runtime.Modules.Call(
                context,
                EtsQueries414Names.Ets,
                function,
                args
            );
            foreach (string kind in new[] { EtsQueries414Names.Set, EtsQueries414Names.Ordered,
                EtsQueries414Names.Bag, EtsQueries414Names.Duplicate })
            {
                Term table = await Call(
                    EtsQueries414Names.New,
                    Term.A(EtsQueries414Names.Table),
                    Term.List(Term.A(EtsQueries414Names.Named), Term.A(EtsQueries414Names.Public), Term.A(kind))
                );
                try
                {
                    Term rows = Term.List(Term.Tuple(Term.I(3), Term.I(30)), Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)));
                    _ = await Call(EtsQueries414Names.Insert, table, rows);
                    Require((await Call(EtsQueries414Names.Count, table, Specification(Term.A(ErlangBooleanAtoms.True)))).Equals(Term.I(3)));
                    Require((await Call(EtsQueries414Names.Count, table, Specification(Term.A(ErlangBooleanAtoms.False)))).Equals(Term.I(0)));
                    Require((await Call(EtsQueries414Names.Count, table, Specification(Term.I(42)))).Equals(Term.I(0)));
                    Require((await Call(EtsQueries414Names.Count, table, Nil.Value)).Equals(Term.I(0)));
                    Require((await Call(EtsQueries414Names.Match, table, Term.Tuple(Term.I(2), Term.A(MatchSpecNames.Wildcard))))
                        .Equals(Term.List(Term.Tuple(Term.I(2), Term.I(20)))));
                    Term reverse = await Call(EtsQueries414Names.Reverse, table, Specification(Term.A(MatchSpecNames.Whole)));
                    Require(Cons.Items(reverse).Count() == 3);
                    if (kind == EtsQueries414Names.Ordered)
                        Require(reverse.Equals(Term.List(Term.Tuple(Term.I(3), Term.I(30)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(1), Term.I(10)))));
                }
                finally
                {
                    _ = await Call(EtsQueries414Names.Delete, table);
                }
            }

            return true;
        });
    }

    internal static async Task Permissions()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsQueries414.Register(runtime, runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string access in new[] { EtsQueries414Names.Private, EtsQueries414Names.Protected })
        {
            Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsQueries414Names.Ets,
                EtsQueries414Names.New,
                Term.A(EtsQueries414Names.Table),
                Term.List(Term.A(access))
            ));
            Term row = Term.Tuple(Term.I(1), Term.I(10));
            _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsQueries414Names.Ets,
                EtsQueries414Names.Insert,
                table,
                row
            ));
            try
            {
                foreach (string function in new[] { EtsQueries414Names.Match, EtsQueries414Names.Count, EtsQueries414Names.Reverse })
                {
                    bool refused = await other.InvokeAsync(async c =>
                    {
                        try
                        {
                            _ = await runtime.Modules.Call(
                                c,
                                EtsQueries414Names.Ets,
                                function,
                                table,
                                function == EtsQueries414Names.Match ? Term.A(MatchSpecNames.Wildcard) : Specification(Term.A(ErlangBooleanAtoms.True))
                            );

                            return false;
                        }
                        catch (ErlangException e) when (e.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                        {
                            return true;
                        }
                    });
                    Require(refused == (access == EtsQueries414Names.Private));
                }
                await owner.InvokeAsync(async c =>
                {
                    foreach (string function in new[] { EtsQueries414Names.Count, EtsQueries414Names.Reverse })
                        await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                            c,
                            EtsQueries414Names.Ets,
                            function,
                            table,
                            Term.A(EtsQueries414Names.Invalid)
                        ));
                    Require((await runtime.Modules.Call(
                        c,
                        EtsQueries414Names.Ets,
                        EtsQueries414Names.Lookup,
                        table,
                        Term.I(1)
                    )).Equals(Term.List(row)));

                    return true;
                });
            }
            finally
            {
                _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                    c,
                    EtsQueries414Names.Ets,
                    EtsQueries414Names.Delete,
                    table
                ));
            }
        }
    }

    internal static async Task Registration()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        await using var foreign = new ProcessRuntime();
        bool foreignRefused = false;
        try
        {
            RuntimeBootstrapEtsQueries414.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            foreignRefused = true;
        }
        Require(foreignRefused);
        ErlangFunction occupied = (_, _) => ValueTask.FromResult<Term>(Nil.Value);
        runtime.Modules.Register(
            EtsQueries414Names.Ets,
            EtsQueries414Names.Reverse,
            2,
            occupied
        );
        bool conflictRefused = false;
        try
        {
            RuntimeBootstrapEtsQueries414.Register(runtime, runtime.Modules);
        }
        catch (InvalidOperationException)
        {
            conflictRefused = true;
        }
        Require(conflictRefused && !runtime.Modules.HasExport(EtsQueries414Names.Ets, EtsQueries414Names.Match, 2)
            && !runtime.Modules.HasExport(EtsQueries414Names.Ets, EtsQueries414Names.Count, 2)
            && runtime.Modules.HasExport(EtsQueries414Names.Ets, EtsQueries414Names.Reverse, 2)
            && runtime.NativePrimitives.Bindings.Count == 11);
    }
}

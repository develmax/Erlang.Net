using Erlang;
namespace Erlang.Tests;

internal static class EtsMutations414Checks
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsMutations414Names.Failure);
    }

    internal static void Register(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsMutations414.Register(runtime, runtime.Modules);
    }

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        RuntimeBootstrapEtsMutations414.Register(runtime, runtime.Modules);
        Require(runtime.NativePrimitives.Bindings.Count == 11);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            async Task<Term> Call(string f, params Term[] args) => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                f,
                args
            );
            foreach (string kind in new[] { EtsMutations414Names.Set, EtsMutations414Names.Ordered, EtsMutations414Names.Bag, EtsMutations414Names.Duplicate })
            {
                Term table = await Call(EtsMutations414Names.New, Term.A(EtsMutations414Names.Table), Term.List(Term.A(kind)));
                try
                {
                    Term row = Term.Tuple(Term.I(1), Term.I(10));
                    Require((await Call(EtsMutations414Names.InsertNew, table, row)).Equals(Term.A(ErlangBooleanAtoms.True)));
                    Require((await Call(EtsMutations414Names.InsertNew, table, Term.List(Term.Tuple(Term.I(2), Term.I(20)), row)))
                        .Equals(Term.A(ErlangBooleanAtoms.False)));
                    Require((await Call(EtsMutations414Names.Lookup, table, Term.I(2))) is Nil);
                    _ = await Call(EtsMutations414Names.Insert, table, Term.List(row, row, Term.Tuple(Term.I(1), Term.I(11))));
                    _ = await Call(EtsMutations414Names.DeleteObject, table, row);
                    Term remaining = await Call(EtsMutations414Names.Lookup, table, Term.I(1));
                    Require(remaining.Equals(Term.List(Term.Tuple(Term.I(1), Term.I(11)))));
                    Require((await Call(EtsMutations414Names.Take, table, Term.I(1))).Equals(remaining));
                    Require((await Call(EtsMutations414Names.Take, table, Term.I(1))) is Nil);
                    if (kind is EtsMutations414Names.Set or EtsMutations414Names.Ordered)
                    {
                        Require((await Call(
                            EtsMutations414Names.Update,
                            table,
                            Term.I(9),
                            Term.A(EtsMutations414Names.Invalid)
                        ))
                            .Equals(Term.A(ErlangBooleanAtoms.False)));
                        _ = await Call(
                            EtsMutations414Names.Update,
                            table,
                            Term.I(2),
                            Nil.Value,
                            Term.Tuple(Term.I(99), Term.I(20))
                        );
                        Require((await Call(EtsMutations414Names.Lookup, table, Term.I(2))).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(20)))));
                        await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                            c,
                            EtsMutations414Names.Ets,
                            EtsMutations414Names.Update,
                            table,
                            Term.I(2),
                            Term.List(Term.Tuple(Term.I(2), Term.I(22)), Term.Tuple(Term.I(1), Term.I(3)))
                        ));
                        Require((await Call(EtsMutations414Names.Lookup, table, Term.I(2))).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(20)))));
                        _ = await Call(
                            EtsMutations414Names.Update,
                            table,
                            Term.I(2),
                            Term.List(Term.Tuple(Term.I(2), Term.I(21)), Term.Tuple(Term.I(2), Term.I(22)))
                        );
                        Require((await Call(EtsMutations414Names.Lookup, table, Term.I(2))).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(22)))));
                    }
                }
                finally
                {
                    _ = await Call(EtsMutations414Names.Delete, table);
                }
            }

            return true;
        });
    }

    internal static async Task HeldReader()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string function in new[] { EtsMutations414Names.Take, EtsMutations414Names.DeleteObject, EtsMutations414Names.Update })
        {
            var state = await actor.InvokeAsync(async c =>
            {
                Term table = await runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.New,
                    Term.A(EtsMutations414Names.Table),
                    Nil.Value
                );
                _ = await runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.Insert,
                    table,
                    Term.Tuple(Term.I(1), Term.I(10))
                );
                object lease = EtsFoundation405Access.Invoke(
                    storage,
                    EtsFoundation405Names.AcquireRead,
                    c,
                    table,
                    1L
                )!;

                return (Table: table, Lease: lease);
            });
            try
            {
                await actor.InvokeAsync(async c =>
                {
                    Term result = function switch
                    {
                        EtsMutations414Names.Take => await runtime.Modules.Call(
                            c,
                            EtsMutations414Names.Ets,
                            function,
                            state.Table,
                            Term.I(1)
                        ),
                        EtsMutations414Names.DeleteObject => await runtime.Modules.Call(
                            c,
                            EtsMutations414Names.Ets,
                            function,
                            state.Table,
                            Term.Tuple(Term.I(1), Term.I(10))
                        ),
                        _ => await runtime.Modules.Call(
                            c,
                            EtsMutations414Names.Ets,
                            function,
                            state.Table,
                            Term.I(1),
                            Term.Tuple(Term.I(2), Term.I(99))
                        )
                    };
                    Term old = (Term)EtsFoundation405Access.Invoke(state.Lease, EtsFoundation405Names.Materialize, CancellationToken.None)!;
                    Require(old.Equals(Term.Tuple(Term.I(1), Term.I(10))));
                    if (function == EtsMutations414Names.Take) Require(result.Equals(Term.List(old)));

                    return true;
                });
            }
            finally
            {
                ((IDisposable)state.Lease).Dispose();
                _ = await actor.InvokeAsync(async c => await runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.Delete,
                    state.Table
                ));
            }
        }
        EtsFoundation405Access.Empty(storage);
    }
}

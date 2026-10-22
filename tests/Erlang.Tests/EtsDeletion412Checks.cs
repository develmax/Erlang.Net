using Erlang;
namespace Erlang.Tests;

internal static class EtsDeletion412Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsDeletion412Names.Failure);
    }

    internal static async Task Kernels()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore);
            RuntimeBootstrapEtsDeletion.Register(runtime, registry);
            RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        });
        // The separate opt-in must not change the existing selected-provider ledger.
        Require(runtime.NativePrimitives.Bindings.Count == 11);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async Task<Term> Call(string function, params Term[] args) => await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                function,
                args
            );
            foreach (string kind in new[] { EtsDeletion412Names.Set, EtsDeletion412Names.Ordered, EtsDeletion412Names.Bag, EtsDeletion412Names.Duplicate })
            {
                Term table = await Call(
                    EtsDeletion412Names.New,
                    Term.A(EtsDeletion412Names.Table),
                    Term.List(Term.A(EtsDeletion412Names.Named), Term.A(EtsDeletion412Names.Public), Term.A(kind))
                );
                try
                {
                    _ = await Call(
                        EtsDeletion412Names.Insert,
                        table,
                        Term.List(
                            Term.Tuple(Term.I(1), Term.A(EtsDeletion412Names.Remove)),
                            Term.Tuple(Term.I(2), Term.A(EtsDeletion412Names.Keep)),
                            Term.Tuple(Term.I(3), Term.A(EtsDeletion412Names.Remove))
                        )
                    );
                    Term pattern = Term.Tuple(Term.A(EtsDeletion412Names.Wildcard), Term.A(EtsDeletion412Names.Remove));
                    Term spec = Term.List(Term.Tuple(pattern, Nil.Value, Term.List(Term.A(ErlangBooleanAtoms.True))));
                    Require((await Call(EtsDeletion412Names.InternalDelete, table, spec)).Equals(Term.I(2)));
                    Require((await Call(EtsDeletion412Names.Lookup, table, Term.I(1))).Equals(Nil.Value));
                    Require((await Call(EtsDeletion412Names.Lookup, table, Term.I(2))).Equals(Term.List(Term.Tuple(Term.I(2), Term.A(EtsDeletion412Names.Keep)))));
                    Require((await Call(EtsDeletion412Names.InternalDelete, table, Nil.Value)).Equals(Term.I(0)));
                    Term falseSpec = Term.List(Term.Tuple(Term.A(EtsDeletion412Names.Wildcard), Nil.Value, Term.List(Term.A(ErlangBooleanAtoms.False))));
                    Require((await Call(EtsDeletion412Names.InternalDelete, table, falseSpec)).Equals(Term.I(0)));
                    Term holder = Term.Tuple(Term.A(EtsDeletion412Names.Holder), Term.I(123));
                    Require((await Call(EtsDeletion412Names.DeleteAll, table, holder)).Equals(holder));
                    Require((await Call(EtsDeletion412Names.DeleteAll, table, Term.A(EtsDeletion412Names.Undefined))).Equals(Term.I(1)));
                    Require((await Call(EtsDeletion412Names.DeleteAll, table, Term.A(EtsDeletion412Names.Undefined))).Equals(Term.I(0)));
                }
                finally
                {
                    _ = await Call(EtsDeletion412Names.Delete, table);
                }
            }

            return true;
        });
    }

    internal static async Task InvalidDoesNotMutate()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore);
            RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term table = await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.New,
                Term.A(EtsDeletion412Names.Table),
                Term.List(Term.A(EtsDeletion412Names.Named), Term.A(EtsDeletion412Names.Private))
            );
            Term value = Term.Tuple(Term.I(1), Term.A(EtsDeletion412Names.Keep));
            _ = await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.Insert,
                table,
                value
            );
            bool invalid = false;
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    EtsDeletion412Names.Ets,
                    EtsDeletion412Names.InternalDelete,
                    table,
                    Term.A(EtsDeletion412Names.Invalid)
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                invalid = true;
            }
            Require(invalid);
            Require((await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.Lookup,
                table,
                Term.I(1)
            )).Equals(Term.List(value)));

            return true;
        });
    }
}

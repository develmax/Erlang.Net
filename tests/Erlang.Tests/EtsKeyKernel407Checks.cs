using Erlang;

namespace Erlang.Tests;

internal static class EtsKeyKernel407Checks
{
    private static Term Call(object storage, string name, params object?[] args)
        => (Term)EtsFoundation405Access.Invoke(storage, name, args)!;

    private static async Task<Term> Mutate(object storage, string name, params object?[] args)
        => await ((ValueTask<Term>)EtsFoundation405Access.Invoke(storage, name, args)!);

    private static async Task Delete(object storage, ProcessContext context, Term table)
    {
        var operation = (ValueTask<Term>)EtsFoundation405Access.Invoke(
            storage,
            EtsKeyKernel407Names.DeleteTable,
            context,
            table
        )!;
        EtsFoundation405Access.Require((await operation).Equals(Term.A(ErlangBooleanAtoms.True)));
    }

    internal static async Task CodeServerDefaultVectors()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term name = Term.A(EtsKeyKernel407Names.Name);
            Term named = Call(
                storage,
                EtsKeyKernel407Names.New,
                context,
                name,
                Term.List(Term.A(EtsKeyKernel407Names.TableKind), Term.A(EtsKeyKernel407Names.Protected))
            );
            EtsFoundation405Access.Require(named.Equals(name));
            Term handle = Call(
                storage,
                EtsKeyKernel407Names.Whereis,
                context,
                name
            );
            EtsFoundation405Access.Require(handle is ReferenceTerm);
            var tuple = Term.Tuple(Term.A(EtsKeyKernel407Names.NameDb), Term.I(407));
            EtsFoundation405Access.Require((await Mutate(
                storage,
                EtsKeyKernel407Names.Insert,
                context,
                handle,
                tuple
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            EtsFoundation405Access.Require(Call(
                storage,
                EtsKeyKernel407Names.Lookup,
                context,
                name,
                tuple.Items[0]
            ).Equals(Term.List(tuple)));
            EtsFoundation405Access.Require(Call(
                storage,
                EtsKeyKernel407Names.Member,
                context,
                handle,
                tuple.Items[0]
            ).Equals(Term.A(ErlangBooleanAtoms.True)));
            Term unnamed = Call(
                storage,
                EtsKeyKernel407Names.New,
                context,
                Term.A(EtsKeyKernel407Names.NameDb),
                Nil.Value
            );
            EtsFoundation405Access.Require(unnamed is ReferenceTerm);
            await Delete(storage, context, unnamed);
            await Delete(storage, context, handle);
            EtsFoundation405Access.Require(Call(
                storage,
                EtsKeyKernel407Names.Whereis,
                context,
                name
            ).Equals(Term.A(EtsKeyKernel407Names.Undefined)));

            return true;
        });
        EtsFoundation405Access.Empty(storage);
        EtsFoundation405Access.Require(!runtime.Modules.HasExport("ets", "new", 2));
    }

    internal static async Task FourFamiliesAndAtomicBatch()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string kind in EtsKeyKernel407Names.Kinds)
            {
                Term table = Call(
                    storage,
                    EtsKeyKernel407Names.New,
                    context,
                    Term.A(EtsKeyKernel407Names.Name),
                    Term.List(Term.A(kind))
                );
                var first = Term.Tuple(Term.I(1), Term.I(1));
                var second = Term.Tuple(new FloatTerm(1.0), Term.I(2));
                _ = await Mutate(
                    storage,
                    EtsKeyKernel407Names.Insert,
                    context,
                    table,
                    Term.List(first, second, first)
                );
                Term rows = Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    table,
                    Term.I(1)
                );
                int expected = kind == "duplicate_bag" ? 2 : 1;
                EtsFoundation405Access.Require(Cons.Items(rows).Count() == expected);
                int floats = Cons.Items(Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    table,
                    new FloatTerm(1.0)
                )).Count();
                EtsFoundation405Access.Require(floats == 1);
                Term before = Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    table,
                    Term.I(1)
                );
                var accounting = EtsFoundation405Access.Snapshot(storage);
                bool invalid = false;
                try
                {
                    _ = await Mutate(
                        storage,
                        EtsKeyKernel407Names.Insert,
                        context,
                        table,
                        Term.List(Term.Tuple(Term.I(1), Term.I(99)), Term.I(7))
                    );
                }
                catch (Exception error) when (error.GetType().Name == EtsKeyKernel407Names.FailureType)
                {
                    invalid = true;
                }
                EtsFoundation405Access.Require(invalid && before.Equals(Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    table,
                    Term.I(1)
                ))
                    && accounting.Equals(EtsFoundation405Access.Snapshot(storage)));
                _ = await Mutate(
                    storage,
                    EtsKeyKernel407Names.DeleteKey,
                    context,
                    table,
                    Term.I(1)
                );
                EtsFoundation405Access.Require(Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    table,
                    Term.I(1)
                ) is Nil);
                await Delete(storage, context, table);
            }

            return true;
        });
        EtsFoundation405Access.Empty(storage);
    }

    internal static async Task ProtectionAndPublicDelete()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var peer = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term publicTable = await owner.Invoke(context => Call(
            storage,
            EtsKeyKernel407Names.New,
            context,
            Term.A(EtsKeyKernel407Names.Name),
            Term.List(Term.A(EtsKeyKernel407Names.Public))
        ));
        await peer.InvokeAsync(async context =>
 {
     await Delete(storage, context, publicTable);

     return true;
 });
        Term privateTable = await owner.Invoke(context => Call(
            storage,
            EtsKeyKernel407Names.New,
            context,
            Term.A(EtsKeyKernel407Names.Name),
            Term.List(Term.A(EtsKeyKernel407Names.Private), Term.A(EtsKeyKernel407Names.Protected))
        ));
        await peer.Invoke(context =>
        {
            bool refused = false;
            try
            {
                _ = Call(
                    storage,
                    EtsKeyKernel407Names.Lookup,
                    context,
                    privateTable,
                    Term.I(1)
                );
            }
            catch (Exception error) when (error.GetType().Name == EtsKeyKernel407Names.FailureType)
            {
                refused = true;
            }
            EtsFoundation405Access.Require(refused);

            return true;
        });
        await owner.InvokeAsync(async context =>
 {
     await Delete(storage, context, privateTable);

     return true;
 });
        EtsFoundation405Access.Empty(storage);
    }
}

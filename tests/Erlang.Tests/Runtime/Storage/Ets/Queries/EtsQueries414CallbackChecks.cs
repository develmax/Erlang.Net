using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsQueries414CallbackChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsQueries414Names.Failure);
    }

    internal static async Task ReverseBeforeCallback()
    {
        await Check(true);
        await Check(false);
    }

    private static async Task Check(bool reverse)
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsQueries414.Register(runtime, runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsQueries414Names.Ets,
            EtsQueries414Names.New,
            Term.A(EtsQueries414Names.Table),
            Term.List(Term.A(EtsQueries414Names.Ordered))
        ));
        _ = await actor.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsQueries414Names.Ets,
            EtsQueries414Names.Insert,
            table,
            Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(3), Term.I(30)))
        ));
        object gate = typeof(ProcessRuntime).GetField(EtsQueries414Names.Gate, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        _ = runtime.Modules.DeleteModule(EtsQueries414Names.Erlang);
        var order = new List<Term>();
        Task<Term>? mutation = null;
        runtime.Modules.Register(
            EtsQueries414Names.Erlang,
            EtsQueries414Names.Predicate,
            1,
            (context, args) =>
        {
            Require(!Monitor.IsEntered(gate));
            order.Add(args[0]);
            if (order.Count == 1)
                mutation = runtime.Modules.Call(
                    context,
                    EtsQueries414Names.Ets,
                    EtsQueries414Names.Insert,
                    table,
                    Term.Tuple(Term.I(3), Term.I(99))
                ).AsTask();

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        }
        );
        Term program = Term.List(Term.Tuple(
            Term.Tuple(Term.A(EtsQueries414Names.First), Term.A(EtsQueries414Names.Second)),
            Term.List(Term.Tuple(Term.A(EtsQueries414Names.Predicate), Term.A(EtsQueries414Names.First))),
            Term.List(Term.A(EtsQueries414Names.First))
        ));
        Term actual = await actor.InvokeAsync(async c =>
        {
            try
            {
                return await runtime.Modules.Call(
                    c,
                    EtsQueries414Names.Ets,
                    reverse ? EtsQueries414Names.Reverse : EtsQueries414Names.Select,
                    table,
                    program
                );
            }
            finally
            {
                if (mutation is not null) _ = await mutation;
            }
        });
        Term[] expectedOrder = reverse ? [Term.I(1), Term.I(2), Term.I(3)] : [Term.I(3), Term.I(2), Term.I(1)];
        Require(actual.Equals(reverse ? Term.List(Term.I(3), Term.I(2), Term.I(1)) : Term.List(Term.I(1), Term.I(2), Term.I(3)))
            && order.SequenceEqual(expectedOrder)
            && mutation is { IsCompletedSuccessfully: true });
        Require((await actor.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsQueries414Names.Ets,
            EtsQueries414Names.Lookup,
            table,
            Term.I(3)
        ))).Equals(Term.List(Term.Tuple(Term.I(3), Term.I(99)))));
        _ = await actor.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsQueries414Names.Ets,
            EtsQueries414Names.Delete,
            table
        ));
    }
}

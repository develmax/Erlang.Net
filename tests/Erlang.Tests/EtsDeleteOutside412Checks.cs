using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class EtsDeleteOutside412Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsDeleteOutside412Names.Failure);
    }

    private static Term Row(string key, int value) => Term.Tuple(Term.A(key), Term.I(value));

    private static Term Specification() => Term.List(Term.Tuple(
        Term.Tuple(Term.A(EtsDeleteOutside412Names.First), Term.A(EtsDeleteOutside412Names.Second)),
        Term.List(Term.Tuple(Term.A(EtsDeleteOutside412Names.Predicate), Term.A(EtsDeleteOutside412Names.First))),
        Term.List(Term.A(EtsDeleteOutside412Names.True))
    ));

    private static Term Sync(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] arguments
    )
    {
        ValueTask<Term> pending = runtime.Modules.Call(
            context,
            EtsDeleteOutside412Names.Ets,
            function,
            arguments
        );
        // Read-only lookups in this helper are synchronous. The callback below
        // separately retains and awaits any asynchronous mutation retirement.
        Require(pending.IsCompletedSuccessfully);

        return pending.Result;
    }

    internal static Task ReentryAndUnchanged() => Run(replace: false);

    internal static Task ReplacedPayload() => Run(replace: true);

    private static async Task Run(bool replace)
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsDeletion.Register(runtime, runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsDeleteOutside412Names.Ets,
            EtsDeleteOutside412Names.New,
            Term.A(EtsDeleteOutside412Names.Table),
            Nil.Value
        ));
        _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsDeleteOutside412Names.Ets,
            EtsDeleteOutside412Names.Insert,
            table,
            Term.List(Row(EtsDeleteOutside412Names.A, 1), Row(EtsDeleteOutside412Names.B, 2))
        ));
        object gate = typeof(ProcessRuntime).GetField(EtsDeleteOutside412Names.Gate,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        _ = runtime.Modules.DeleteModule(EtsDeleteOutside412Names.Erlang);
        int calls = 0;
        Task<Term>? mutation = null;
        runtime.Modules.Register(
            EtsDeleteOutside412Names.Erlang,
            EtsDeleteOutside412Names.Predicate,
            1,
            (context, _) =>
            {
                Require(!Monitor.IsEntered(gate));
                if (++calls == 1)
                {
                    ValueTask<Term> pending = runtime.Modules.Call(
                        context,
                        EtsDeleteOutside412Names.Ets,
                        EtsDeleteOutside412Names.Insert,
                        table,
                        Row(replace ? EtsDeleteOutside412Names.A : EtsDeleteOutside412Names.C, 9)
                    );
                    // Insert's real pointer commit occurs before its possible
                    // retirement await. Do not synchronously wait on the actor.
                    mutation = pending.AsTask();
                }

                return ValueTask.FromResult<Term>(Term.A(EtsDeleteOutside412Names.True));
            }
        );
        Term removed = await actor.InvokeAsync(async context =>
        {
            Term result;
            try
            {
                result = await runtime.Modules.Call(
                    context,
                    EtsDeleteOutside412Names.Ets,
                    EtsDeleteOutside412Names.SelectDelete,
                    table,
                    Specification()
                );
            }
            finally
            {
                if (mutation is not null) _ = await mutation;
            }

            return result;
        });
        Require(calls == 2 && mutation is { IsCompletedSuccessfully: true }
            && removed.Equals(Term.I(replace ? 1 : 2)));
        Term retained = await actor.InvokeAsync(context => Task.FromResult(Sync(
            runtime,
            context,
            EtsDeleteOutside412Names.Lookup,
            table,
            Term.A(replace ? EtsDeleteOutside412Names.A : EtsDeleteOutside412Names.C)
        )));
        Require(retained.Equals(Term.List(Row(replace ? EtsDeleteOutside412Names.A : EtsDeleteOutside412Names.C, 9))));
        Term missing = await actor.InvokeAsync(context => Task.FromResult(Sync(
            runtime,
            context,
            EtsDeleteOutside412Names.Lookup,
            table,
            Term.A(EtsDeleteOutside412Names.B)
        )));
        Require(missing is Nil);
    }
}

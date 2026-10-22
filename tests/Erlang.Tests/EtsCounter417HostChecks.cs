using System.Numerics;

namespace Erlang.Tests;

internal static class EtsCounter417HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsCounter417HostNames.Failure);
    }

    private static void Register(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsCounter417.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] arguments
    ) => runtime.Modules.Call(
        context,
        EtsCounter417HostNames.Ets,
        function,
        arguments
    );

    internal static async Task Basics()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A(EtsCounter417HostNames.Key);
            Term table = await Call(
                runtime,
                context,
                EtsCounter417HostNames.New,
                Term.A(EtsCounter417HostNames.Table),
                Nil.Value
            );
            try
            {
                _ = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Insert,
                    table,
                    Term.Tuple(key, Term.I(10), Term.I(20))
                );
                Term value = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    table,
                    key,
                    Term.List(
                        Term.Tuple(Term.I(2), Term.I(1)),
                        Term.Tuple(Term.I(2), Term.I(-3)),
                        Term.Tuple(
                            Term.I(2),
                            Term.I(100),
                            Term.I(20),
                            Term.I(-5)
                        )
                    )
                );
                Require(value.Equals(Term.List(Term.I(11), Term.I(8), Term.I(-5))));
                BigInteger big = BigInteger.One << 100;
                value = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    table,
                    key,
                    new Integer(big)
                );
                Require(value.Equals(new Integer(big - 5)));
                Term missing = Term.A(EtsCounter417HostNames.Missing);
                value = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    table,
                    missing,
                    Nil.Value,
                    Term.Tuple(key, Term.I(3))
                );
                Require(value is Nil);
                Require((await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Lookup,
                    table,
                    missing
                ))
                    .Equals(Term.List(Term.Tuple(missing, Term.I(3)))));
            }
            finally
            {
                _ = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Delete,
                    table
                );
            }

            return true;
        });
    }

    internal static async Task AtomicAndRollback()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term key = Term.A(EtsCounter417HostNames.Key);
        Term table = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCounter417HostNames.New,
            Term.A(EtsCounter417HostNames.Table),
            Term.List(Term.A(EtsCounter417HostNames.Public))
        ));
        var writers = new List<RuntimeNodeFoundationHostActor>();
        try
        {
            await owner.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCounter417HostNames.Insert,
                table,
                Term.Tuple(key, Term.I(0), Term.I(7))
            ));
            for (int index = 0; index < EtsCounter417HostNames.Writers; index++)
                writers.Add(new RuntimeNodeFoundationHostActor(runtime));
            Task<Term[]>[] tasks = writers.Select(writer => writer.InvokeAsync(async context =>
            {
                var results = new Term[EtsCounter417HostNames.Increments];
                for (int index = 0; index < results.Length; index++)
                    results[index] = await Call(
                        runtime,
                        context,
                        EtsCounter417HostNames.Update,
                        table,
                        key,
                        Term.I(1)
                    );

                return results;
            })).ToArray();
            Term[][] results = await Task.WhenAll(tasks);
            int total = EtsCounter417HostNames.Writers * EtsCounter417HostNames.Increments;
            Require(results.SelectMany(values => values).Select(value => ((Integer)value).Value).ToHashSet().Count == total);
            await owner.InvokeAsync(async context =>
            {
                Term before = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Lookup,
                    table,
                    key
                );
                Require(before.Equals(Term.List(Term.Tuple(key, Term.I(total), Term.I(7)))));
                await EtsSelect409Access.BadArgument(() => Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    table,
                    key,
                    Term.List(Term.Tuple(Term.I(2), Term.I(5)),
                        Term.Tuple(Term.I(3), Term.A(EtsCounter417HostNames.Invalid)))
                ));
                Require(before.Equals(await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Lookup,
                    table,
                    key
                )));
                Term missing = Term.A(EtsCounter417HostNames.Missing);
                await EtsSelect409Access.BadArgument(() => Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    table,
                    missing,
                    Term.List(Term.Tuple(Term.I(2), Term.I(1)), Term.I(42)),
                    Term.Tuple(key, Term.I(3))
                ));
                Require(await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Lookup,
                    table,
                    missing
                ) is Nil);

                return true;
            });
        }
        finally
        {
            foreach (var writer in writers) await writer.DisposeAsync();
            await owner.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCounter417HostNames.Delete,
                table
            ));
        }
    }

    internal static async Task Admission()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        int count = runtime.Modules.Exports.Count();
        RuntimeBootstrapEtsCounter417.Register(runtime, runtime.Modules);
        Require(count == runtime.Modules.Exports.Count() && runtime.NativePrimitives.Bindings.Count == EtsCounter417HostNames.ProviderCount);
        await using var foreign = new ProcessRuntime();
        int before = foreign.Modules.Exports.Count();
        bool denied = false;
        try
        {
            RuntimeBootstrapEtsCounter417.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            denied = true;
        }
        Require(denied && before == foreign.Modules.Exports.Count());
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        await actor.InvokeAsync(async context =>
        {
            bool rejected = false;
            try
            {
                _ = await Call(
                    runtime,
                    context,
                    EtsCounter417HostNames.Update,
                    Nil.Value,
                    Nil.Value,
                    Nil.Value
                );
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Require(rejected);

            return true;
        });
        await using var conflict = new ProcessRuntime();
        conflict.Modules.Register(
            EtsCounter417HostNames.Ets,
            EtsCounter417HostNames.Update,
            4,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsCounter417.Register(conflict, conflict.Modules);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused && !conflict.Modules.HasExport(EtsCounter417HostNames.Ets, EtsCounter417HostNames.Update, 3));
    }
}

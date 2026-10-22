using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsReplace420Checks
{
    private static void Require(bool ok)
    {
        if (!ok) throw new InvalidOperationException(EtsReplace420Names.Failure);
    }

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsReplace420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] arguments
    ) => runtime.Modules.Call(
        context,
        EtsReplace420Names.Module,
        function,
        arguments
    );

    private static Term Spec(Term key, Term guards) => Term.List(Term.Tuple(
        Term.Tuple(Term.A(EtsReplace420Names.Variable), Term.A(EtsReplace420Names.Wildcard)),
        guards,
        Term.List(Term.Tuple(Term.Tuple(key, Term.I(42))))
    ));

    private static async Task<Term> Create(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor) => await actor.InvokeAsync(async context => await Call(
        runtime,
        context,
        EtsReplace420Names.New,
        Term.A(EtsReplace420Names.Name),
        Term.List(Term.A(EtsReplace420Names.Named), Term.A(EtsReplace420Names.Public), Term.A(EtsReplace420Names.Ordered))
    ));

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(1);
        Require(!runtime.Modules.HasExport(EtsReplace420Names.Module, EtsReplace420Names.Function, 2));
        RuntimeBootstrapEtsReplace420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsReplace420.Register(runtime, runtime.Modules);
        Require(runtime.Modules.HasExport(EtsReplace420Names.Module, EtsReplace420Names.Function, 2) && runtime.Modules.SourceOwner(EtsReplace420Names.Module, EtsReplace420Names.Function, 2) is null);
        await using var foreign = new ProcessRuntime(1);
        bool rejected = false;
        try
        {
            RuntimeBootstrapEtsReplace420.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var rollback = new ProcessRuntime(1);
        var expected = new InvalidOperationException(EtsReplace420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsReplace420.Register(rollback, registry);
                throw expected;
            });
        }
        catch (InvalidOperationException seen) when (ReferenceEquals(seen, expected)) { }
        Require(!rollback.Modules.HasExport(EtsReplace420Names.Module, EtsReplace420Names.Function, 2));
    }

    internal static async Task Basics()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await Create(runtime, owner);
        await owner.InvokeAsync(async context =>
        {
            _ = await Call(
                runtime,
                context,
                EtsReplace420Names.Insert,
                table,
                Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)))
            );
            bool rejected = false;
            try
            {
                _ = await Call(
                    runtime,
                    context,
                    EtsReplace420Names.Function,
                    table,
                    Spec(Term.I(99), Nil.Value)
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(EtsReplace420Names.Badarg)))
            {
                rejected = true;
            }
            Require(rejected);
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(1)
            )).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(10)))));
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(2)
            )).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(20)))));
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Function,
                table,
                Spec(Term.A(EtsReplace420Names.Variable), Nil.Value)
            )).Equals(Term.I(2)));
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(1)
            )).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(42)))));

            return true;
        });
    }

    internal static async Task Mutable()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var writer = new RuntimeNodeFoundationHostActor(runtime);
        _ = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsReplace420Names.Erlang,
            EtsReplace420Names.Flag,
            Term.A(EtsReplace420Names.Scheduler),
            Term.I(1)
        ));
        _ = await writer.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsReplace420Names.Erlang,
            EtsReplace420Names.Flag,
            Term.A(EtsReplace420Names.Scheduler),
            Term.I(2)
        ));
        Term table = await Create(runtime, owner);
        object gate = typeof(ProcessRuntime).GetField(EtsReplace420Names.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        object storage = EtsFoundation405Access.Get(runtime);
        _ = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsReplace420Names.Insert,
            table,
            Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(3), Term.I(30)))
        ));
        runtime.Modules.DeleteModule(EtsReplace420Names.Erlang);
        int calls = 0;
        runtime.Modules.Register(
            EtsReplace420Names.Erlang,
            EtsReplace420Names.IsInteger,
            1,
            (callbackContext, arguments) =>
        {
            Require(!Monitor.IsEntered(gate));
            if (Interlocked.Increment(ref calls) == 1)
            {
                Task operation;
                using (ExecutionContext.SuppressFlow()) operation = Task.Run(async () =>
          {
              _ = await writer.InvokeAsync(async context => await Call(
                  runtime,
                  context,
                  EtsReplace420Names.Insert,
                  table,
                  Term.Tuple(Term.I(1), Term.I(999))
              ));
          });
                operation.GetAwaiter().GetResult();
                Require((long)EtsFoundation405Access.Snapshot(storage)[3]! == 3);
            }

            return ValueTask.FromResult<Term>(Term.A(EtsReplace420Names.True));
        }
        );
        Term guards = Term.List(Term.Tuple(Term.A(EtsReplace420Names.IsInteger), Term.A(EtsReplace420Names.Variable)));
        Term count = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsReplace420Names.Function,
            table,
            Spec(Term.A(EtsReplace420Names.Variable), guards)
        ));
        Require(count.Equals(Term.I(2)) && calls == 3);
        await owner.InvokeAsync(async context =>
        {
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(1)
            )).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(999)))));
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(2)
            )).Equals(Term.List(Term.Tuple(Term.I(2), Term.I(42)))));
            Require((await Call(
                runtime,
                context,
                EtsReplace420Names.Lookup,
                table,
                Term.I(3)
            )).Equals(Term.List(Term.Tuple(Term.I(3), Term.I(42)))));

            return true;
        });
    }
}

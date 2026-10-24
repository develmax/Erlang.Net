using Erlang;

namespace Erlang.Tests;

internal static class EtsDeletion412AccessChecks
{
    internal static async Task Permissions()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore);
            RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        });
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string access in new[] { EtsDeletion412Names.Private, EtsDeletion412Names.Protected })
        {
            Term table = await owner.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.New,
                Term.A(EtsDeletion412Names.Table),
                Term.List(Term.A(access))
            ));
            Term value = Term.Tuple(Term.I(1), Term.A(EtsDeletion412Names.Keep));
            _ = await owner.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.Insert,
                table,
                value
            ));
            foreach (string function in new[] { EtsDeletion412Names.InternalDelete, EtsDeletion412Names.DeleteAll })
            {
                bool refused = await other.InvokeAsync(async context =>
                {
                    try
                    {
                        _ = await runtime.Modules.Call(
                            context,
                            EtsDeletion412Names.Ets,
                            function,
                            table,
                            Term.A(EtsDeletion412Names.Undefined)
                        );
                    }
                    catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                        return true;
                    }

                    return false;
                });
                Require(refused);
            }
            Term retained = await owner.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.Lookup,
                table,
                Term.I(1)
            ));
            Require(retained.Equals(Term.List(value)));
            _ = await owner.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                EtsDeletion412Names.Ets,
                EtsDeletion412Names.Delete,
                table
            ));
        }
    }

    internal static async Task Registration()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.Register(
            runtime, registry, RuntimeNativePrimitives.EtsStorageCore));
        await using var foreign = new ProcessRuntime();
        bool foreignRefused = false;
        try
        {
            RuntimeBootstrapEtsDeletion.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            foreignRefused = true;
        }
        Require(foreignRefused);
        runtime.Modules.Register(
            EtsDeletion412Names.Ets,
            EtsDeletion412Names.DeleteAll,
            2,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        bool conflictRefused = false;
        try
        {
            RuntimeBootstrapEtsDeletion.Register(runtime, runtime.Modules);
        }
        catch (InvalidOperationException)
        {
            conflictRefused = true;
        }
        Require(conflictRefused
            && !runtime.Modules.HasExport(EtsDeletion412Names.Ets, EtsDeletion412Names.InternalDelete, 2)
            && runtime.Modules.HasExport(EtsDeletion412Names.Ets, EtsDeletion412Names.DeleteAll, 2)
            && runtime.NativePrimitives.Bindings.Count == 11);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsDeletion412Names.Failure);
    }
}

using Erlang;
namespace Erlang.Tests;

internal static class EtsMutations414AdmissionChecks
{
    internal static async Task Rights()
    {
        await using var runtime = new ProcessRuntime();
        EtsMutations414Checks.Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsMutations414Names.Ets,
            EtsMutations414Names.New,
            Term.A(EtsMutations414Names.Table),
            Term.List(Term.A(EtsMutations414Names.Protected))
        ));
        Term row = Term.Tuple(Term.I(1), Term.I(10));
        _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsMutations414Names.Ets,
            EtsMutations414Names.Insert,
            table,
            row
        ));
        try
        {
            await other.InvokeAsync(async c =>
            {
                foreach (string function in new[] { EtsMutations414Names.InsertNew, EtsMutations414Names.Take, EtsMutations414Names.DeleteObject })
                    await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                        c,
                        EtsMutations414Names.Ets,
                        function,
                        table,
                        function == EtsMutations414Names.Take ? Term.I(1) : row
                    ));
                await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.Update,
                    table,
                    Term.I(1),
                    Term.Tuple(Term.I(2), Term.I(11))
                ));
                await EtsSelect409Access.BadArgument(() => runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.Update,
                    table,
                    Term.I(1),
                    Term.Tuple(Term.I(2), Term.I(11)),
                    row
                ));

                return true;
            });
            EtsMutations414Checks.Require((await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Lookup,
                table,
                Term.I(1)
            ))).Equals(Term.List(row)));
        }
        finally
        {
            _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Delete,
                table
            ));
        }
    }

    internal static async Task Registration()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        await using var foreign = new ProcessRuntime();
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsMutations414.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        EtsMutations414Checks.Require(refused);
        runtime.Modules.Register(
            EtsMutations414Names.Ets,
            EtsMutations414Names.Update,
            4,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        refused = false;
        try
        {
            RuntimeBootstrapEtsMutations414.Register(runtime, runtime.Modules);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        EtsMutations414Checks.Require(refused && runtime.NativePrimitives.Bindings.Count == 11
            && !runtime.Modules.HasExport(EtsMutations414Names.Ets, EtsMutations414Names.InsertNew, 2)
            && !runtime.Modules.HasExport(EtsMutations414Names.Ets, EtsMutations414Names.Take, 2)
            && !runtime.Modules.HasExport(EtsMutations414Names.Ets, EtsMutations414Names.DeleteObject, 2)
            && !runtime.Modules.HasExport(EtsMutations414Names.Ets, EtsMutations414Names.Update, 3)
            && runtime.Modules.HasExport(EtsMutations414Names.Ets, EtsMutations414Names.Update, 4));
    }
}

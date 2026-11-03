using System.Reflection;

namespace Erlang.Tests;

internal static class StatisticsTrapNativeCaptureChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        await using var foreign = new ProcessRuntime(2);
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        var image = (PreparedBeamImage)typeof(ModuleRegistry).GetField(StatisticsTrapNativeCaptureNames.Image, members)!
            .GetValue(runtime.Modules)!;
        object identity = typeof(PreparedBeamImage).GetProperty(StatisticsTrapNativeCaptureNames.Identity, members)!.GetValue(image)!;
        var primitives = (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)identity.GetType()
            .GetProperty(StatisticsTrapNativeCaptureNames.Primitives, members)!.GetValue(identity)!;
        foreach (var signature in StatisticsTrapNativeCaptureNames.Required)
        {
            Require(image.NativePrimitiveExports.Any(value => value.Name == signature.Function && value.Arity == signature.Arity));
            Require(primitives.ContainsKey(signature));
            Require(!runtime.Modules.Exports.Any(value => value.Module == StatisticsTrapNativeCaptureNames.Module
                && value.Function == signature.Function && value.Arity == signature.Arity));
            Require(runtime.Modules.SourceOwner(StatisticsTrapNativeCaptureNames.Module, signature.Function, signature.Arity) is null);
        }
        object capabilities = identity.GetType().GetProperty(StatisticsTrapNativeCaptureNames.Capabilities, members)!.GetValue(identity)!;
        MethodInfo lookup = capabilities.GetType().GetMethod(StatisticsTrapNativeCaptureNames.TryGetValue)!;
        foreach (var signature in StatisticsTrapNativeCaptureNames.Required)
        {
            object?[] selected = [primitives[signature], null];
            bool found = (bool)lookup.Invoke(capabilities, selected)!;
            if (signature.Function == StatisticsTrapNativeCaptureNames.AtomicsNew)
                Require(found && selected[1]!.ToString() == StatisticsTrapNativeCaptureNames.RuntimeRead);
            else
                Require(!found);
        }
        ErlangFunction realConstructor = primitives[(StatisticsTrapNativeCaptureNames.AtomicsNew, 2)];
        ErlangFunction forwardedConstructor = (context, arguments) => realConstructor(context, arguments);
        Require(!(bool)lookup.Invoke(capabilities, [forwardedConstructor, null])!);
        // Actual real bodies retain their own runtime/registry checks. No native
        // port is opened and no wall request is published by a losing foreign call.
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        await actor.InvokeAsync(async context =>
        {
            foreach (var signature in new[]
            {
                ("port_info", 1), (StatisticsTrapNativeCaptureNames.Scheduler, 1),
                ("is_system_process", 1), ("ets_raw_first", 1)
            })
            {
                bool refused = false;
                try
                {
                    await primitives[signature](context, [Term.I(0)]);
                }
                catch (ArgumentException)
                {
                    refused = true;
                }
                Require(refused);
            }

            bool foreignConstructorRefused = false;
            try
            {
                await realConstructor(context, [Term.I(1), Term.I(0)]);
            }
            catch (ArgumentException)
            {
                foreignConstructorRefused = true;
            }
            Require(foreignConstructorRefused);

            return (Term)Nil.Value;
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(StatisticsTrapNativeCaptureNames.Failure);
    }
}

using System.Reflection;
using static Erlang.Tests.CountersCapability596Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class CountersCapability596Checks
{
    internal static Task ExactPublicationAuthority() => CountersCustody596Checks.Run(async fixture =>
    {
        ModuleRegistry registry = fixture.Runtime.Modules;
        ErlangFunction? selected = null;
        await fixture.Runtime.InitializeModulesAsync(actual =>
        {
            PreparedBeamImage image = CountersOriginal421Preparation.Prepare(fixture.Runtime, actual);
            object execution = Property(image, Execution);
            var primitives = (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)Property(execution, Primitives);
            MethodInfo nativeKind = typeof(ModuleRegistry).GetMethod(NativeKind, Members)!;
            foreach (var signature in CountersOriginal421Names.NativeSignatures)
            {
                ErlangFunction primitive = primitives[signature];
                Require(nativeKind.Invoke(actual, [execution, primitive])!.ToString() == TrustedKind);
            }
            selected = primitives[(CountersNative596Names.Get, 2)];
            Require(actual.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        });
        Require(selected is not null);
        await using var purger = new RuntimeNodeFoundationHostActor(fixture.Runtime);
        if (registry.HasOldCode(CountersNative596Names.GuestModule))
        {
            Term purged = await purger.InvokeAsync(async context => await registry.Call(
                context,
                ErlangModuleNames.Module,
                CodePurgeHostNames.Purge,
                Term.A(CountersNative596Names.GuestModule)
            ));
            Require(purged.Equals(Term.A(ErlangBooleanAtoms.True))
                && !registry.HasOldCode(CountersNative596Names.GuestModule));
        }
        _ = registry.DeleteModule(CountersNative596Names.GuestModule);
        registry.Register(
            CountersNative596Names.GuestModule,
            CountersNative596Names.Get,
            2,
            selected!
        );
        // Read policy only: never mutate a capability or create artificial source authority.
        MethodInfo selectedKind = typeof(ModuleRegistry).GetMethod(SelectedKind, Members)!;
        Require(selectedKind.Invoke(registry, [(CountersNative596Names.GuestModule, CountersNative596Names.Get, 2), selected])!.ToString() == OpaqueKind);
    });

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}

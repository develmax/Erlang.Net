using static Erlang.Tests.LibrarySelectionHostData;
using static Erlang.Tests.LibrarySelectionPolicyControlNames;
using static Erlang.Tests.LibrarySelectionPolicyControlBytes;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyControlChecks
{
    internal static async Task ManagedRuntimeDependency()
    {
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(RuntimeModule),
            RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, RuntimeModule, Wrapper), [RuntimeModule])
        ]));
        RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single(value => value.Module == RuntimeModule);
        Require(selected.Backend == RuntimeModuleBackend.Managed);
        Require(!selected.NativePrimitives.Any(value => value.Name == Wrapper && value.Arity == 1));
        Require(runtime.Modules.HasExport(RuntimeModule, Wrapper, 1));
        Require(runtime.Modules.SourceOwner(RuntimeModule, Wrapper, 1) is null);
        Require(runtime.Modules.SourceOwner(PureModule, Run, 1) is PreparedBeamImage);
    }

    internal static async Task NativeBootstrapOwnership()
    {
        foreach (var target in new[] { (StringModule, StringNative), (MathModule, MathNative) })
        {
            byte[] image = NativeStub(target.Item1, target.Item2);
            await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
                RuntimeModuleSelection.Beam(target.Item1, image)
            ]));
            RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single();
            Require(selected.Backend == RuntimeModuleBackend.Beam);
            Require(selected.NativePrimitives.Any(value => value.Name == target.Item2 && value.Arity == 1));
            Require(runtime.Modules.SourceOwner(target.Item1, target.Item2, 1) is PreparedBeamImage);
            Require(!runtime.Modules.HasOldCode(target.Item1));
            PreparedBeamImage reload = runtime.Modules.PrepareBeam(image, new BeamDependencySelection([target.Item1]));
            Require(runtime.Modules.PublishPreparedBeam(reload).Status == BeamPublicationStatus.Published);
            Require(runtime.Modules.HasOldCode(target.Item1));
        }
    }

    internal static async Task ExplicitPureCycle()
    {
        RuntimeModuleSelection first = RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, PeerModule, Run), [PeerModule]);
        RuntimeModuleSelection second = RuntimeModuleSelection.Beam(PeerModule, Identity(PeerModule, PureModule, Run), [PureModule]);
        foreach (bool reverse in new[] { false, true })
        {
            await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection(reverse ? [second, first] : [first, second]));
            foreach (RuntimeLibraryModuleSnapshot selected in runtime.LibrarySelection.Modules)
            {
                Require(selected.Backend == RuntimeModuleBackend.Beam);
                Require(runtime.Modules.SourceOwner(selected.Module, Run, 1) is PreparedBeamImage);
                Require(!runtime.Modules.HasOldCode(selected.Module));
            }
        }
    }

    internal static async Task StrictMissingWrapperSelection()
    {
        bool refused = false;
        try
        {
            await using var unexpected = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
                RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, RuntimeModule, Wrapper))
            ]));
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused);
    }
}

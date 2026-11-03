using static Erlang.Tests.LibrarySelectionHostData;
using static Erlang.Tests.LibrarySelectionPolicyControlNames;
using static Erlang.Tests.LibrarySelectionPolicyControlBytes;
using static Erlang.Tests.LibrarySelectionPolicyNegativeAccess;
using static Erlang.Tests.LibrarySelectionPolicyNegativeNames;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyNegativeChecks
{
    internal static Task FactoryRefusals()
    {
        foreach (string module in new[] { StringModule, MathModule, PureModule })
            RejectFactory(() => RuntimeModuleSelection.Managed(module));
        RejectFactory(() => new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, PeerModule, Run), [PeerModule])
        ]));

        return Task.CompletedTask;
    }

    internal static async Task BootstrapBodyMutation()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            // Deliberately inject stale binding state; test actual policy after releasing M.
            Bodies(registry)[(StringModule, StringNative, 1)] = (_, _) => ValueTask.FromResult<Term>(Term.I(7));
        });
        RejectPrepare(
            runtime.Modules,
            new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(StringModule, NativeStub(StringModule, StringNative))
        ]),
            StringOwnerFailure
        );
    }

    internal static async Task ManagedWrapperUnbound()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            Bodies(registry)[(RuntimeModule, Wrapper, 1)] = (_, _) => ValueTask.FromResult<Term>(Term.I(9));
        });
        RejectPrepare(runtime.Modules,
            new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(RuntimeModule)]), RuntimeOwnerFailure);
    }

    internal static async Task MissingCaptureAndExport()
    {
        await using var runtime = new ProcessRuntime();
        // Same MFA, ordinary identity body: actual missing primitive capture.
        byte[] uncaptured = Identity(StringModule, RuntimeModule, BeamOpcodeEdgeNames.Length);
        uncaptured = RenameRunExport(uncaptured, StringNative);
        RejectPrepare(
            runtime.Modules,
            new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(StringModule, uncaptured)]),
            StringPreparationFailure,
            StringPrimitiveFailure
        );
        // Different export: retain the independent missing-bootstrap-export refusal.
        RejectPrepare(
            runtime.Modules,
            new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(StringModule, Identity(StringModule, RuntimeModule, BeamOpcodeEdgeNames.Length))
        ]),
            StringPreparationFailure,
            StringMissingExportFailure
        );
    }

    internal static async Task SourceOwnerAndOldGeneration()
    {
        await using var runtime = new ProcessRuntime();
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunOutsideAsync(
            runtime,
            async (registry, scope) =>
        {
            byte[] image = Identity(PureModule, RuntimeModule, BeamOpcodeEdgeNames.Length);
            PreparedBeamImage first = scope.Prepare(() => registry.PrepareBeam(image));
            await runtime.InitializeModulesAsync(_ =>
            {
                Require(registry.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published);
                Require(!registry.HasOldCode(PureModule));
            });
            RejectPrepare(registry,
                new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(PureModule, image)]), PureOwnerFailure);
            PreparedBeamImage next = scope.Prepare(() => registry.PrepareBeam(image));
            await runtime.InitializeModulesAsync(_ =>
            {
                Require(registry.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published);
                Require(registry.HasOldCode(PureModule));
            });
            RejectPrepare(registry,
                new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(PureModule, image)]), PureOwnerFailure);
        }
        );
    }

    internal static async Task NativeSourceOwner()
    {
        byte[] image = NativeStub(StringModule, StringNative);
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(StringModule, image)
        ]));
        await runtime.InitializeModulesAsync(registry =>
        {
            var original = (PreparedBeamImage)registry.SourceOwner(StringModule, StringNative, 1)!;
            Require(!registry.HasOldCode(StringModule));
            Require(original.NativePrimitiveExports.Count == 1);
        });
        RejectPrepare(runtime.Modules,
            new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(StringModule, image)]), StringOwnerFailure);
    }

    internal static async Task MissingSelectedMfa()
    {
        await using var runtime = new ProcessRuntime();
        RejectPrepare(
            runtime.Modules,
            new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(RuntimeModule),
            RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, RuntimeModule, MissingFunction), [RuntimeModule])
        ]),
            MissingSelectedMfaFailure
        );
    }

    private static byte[] RenameRunExport(byte[] image, string name)
        => LibrarySelectionPolicyNegativeBytes.RenameRunExport(image, name);
}

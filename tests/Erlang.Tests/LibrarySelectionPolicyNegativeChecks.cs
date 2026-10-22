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
            // Deliberately inject stale binding state. Register itself uses Add and cannot replace a key.
            Bodies(registry)[(StringModule, StringNative, 1)] = (_, _) => ValueTask.FromResult<Term>(Term.I(7));
            RejectPrepare(registry, new RuntimeLibrarySelection([
                RuntimeModuleSelection.Beam(StringModule, NativeStub(StringModule, StringNative))
            ]));
        });
    }

    internal static async Task ManagedWrapperUnbound()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            Bodies(registry)[(RuntimeModule, Wrapper, 1)] = (_, _) => ValueTask.FromResult<Term>(Term.I(9));
            RejectPrepare(registry, new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(RuntimeModule)]));
        });
    }

    internal static async Task MissingCaptureAndExport()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            // Same MFA as the bootstrap, but an ordinary identity body cannot capture its primitive.
            byte[] uncaptured = Identity(StringModule, RuntimeModule, BeamOpcodeEdgeNames.Length);
            uncaptured = RenameRunExport(uncaptured, StringNative);
            RejectPrepare(registry, new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(StringModule, uncaptured)]));
            // Different export: retains the existing missing-bootstrap-export refusal independently.
            RejectPrepare(
                registry,
                new RuntimeLibrarySelection([
                RuntimeModuleSelection.Beam(StringModule, Identity(StringModule, RuntimeModule, BeamOpcodeEdgeNames.Length))
            ])
            );
        });
    }

    internal static async Task SourceOwnerAndOldGeneration()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            byte[] image = Identity(PureModule, RuntimeModule, BeamOpcodeEdgeNames.Length);
            PreparedBeamImage first = registry.PrepareBeam(image);
            Require(registry.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published);
            Require(!registry.HasOldCode(PureModule));
            RejectPrepare(registry, new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(PureModule, image)]));
            PreparedBeamImage next = registry.PrepareBeam(image);
            Require(registry.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published);
            Require(registry.HasOldCode(PureModule));
            RejectPrepare(registry, new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(PureModule, image)]));
        });
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
            RejectPrepare(registry, new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(StringModule, image)]));
        });
    }

    internal static async Task MissingSelectedMfa()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RejectPrepare(
            registry,
            new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(RuntimeModule),
            RuntimeModuleSelection.Beam(PureModule, Identity(PureModule, RuntimeModule, MissingFunction), [RuntimeModule])
        ])
        ));
    }

    private static byte[] RenameRunExport(byte[] image, string name)
        => LibrarySelectionPolicyNegativeBytes.RenameRunExport(image, name);
}

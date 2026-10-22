using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ClosedWaitOriginal412Admission
{
    internal static async Task Run()
    {
        // Separate owner/selection from the strict two-module worker consumer.
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(ClosedWaitOriginal412Names.Runtime)
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        byte[] bytes = ClosedWaitOriginal412Access.Resource(ClosedWaitOriginal412Names.PeerImage);
        string[] imports = JsonSerializer.Deserialize<string[]>(ClosedWaitOriginal412Access.Resource(ClosedWaitOriginal412Names.PeerImports))!;
        BeamFunctionSignature[] expected = JsonSerializer.Deserialize<BeamFunctionSignature[]>(ClosedWaitOriginal412Access.Resource(ClosedWaitOriginal412Names.PeerExports))!;
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports));
        ClosedWaitOriginal412Access.Require(image.Name == ClosedWaitOriginal412Names.PeerModule
            && image.NativePrimitiveExports.Count == 0 && image.RegistryModuleDependencies.SetEquals(imports)
            && !runtime.Modules.IsModuleLoaded(image.Name));
        ClosedWaitOriginal412Access.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        var exports = runtime.Modules.Exports.Where(value => value.Module == image.Name).ToArray();
        bool owners = exports.All(value => ReferenceEquals(runtime.Modules.SourceOwner(value.Module, value.Function, value.Arity), image));
        bool exactExports = exports.Select(value => (value.Function, value.Arity)).ToHashSet()
            .SetEquals(expected.Select(value => (value.Name, value.Arity)));
        ClosedWaitOriginal412Access.Require(image.IsConsumed && owners && exports.Length > 0 && exactExports
            && runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.LibrarySelection.Modules.Count == 1 && runtime.LibrarySelection.Modules.All(module =>
                module.Module == ClosedWaitOriginal412Names.Runtime && module.Backend == RuntimeModuleBackend.Managed));
        string output = Path.Combine(ClosedWaitOriginal412Names.ArtifactRoot, Guid.NewGuid().ToString(ClosedWaitOriginal412Names.GuidFormat));
        Directory.CreateDirectory(output);
        File.WriteAllText(
            Path.Combine(output, ClosedWaitOriginal412Names.AdmissionReceipt),
            JsonSerializer.Serialize(new
            {
                OriginalImageSha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                NativeMd5 = Convert.ToHexString(image.NativeMd5.ToArray()),
                Prepared = true,
                Published = image.IsConsumed,
                SourceOwnersExact = owners,
                ExportsExact = exactExports,
                Exports = exports.Select(value => new { value.Function, value.Arity }),
                DeclaredImportModules = image.RegistryModuleDependencies,
                SelectedModules = runtime.LibrarySelection.Modules.Select(value => new { value.Module, value.Backend }),
                ImageExecuted = false,
                StrictDependencyClosureClaim = false,
                PriorCallerFramesOpaqueAbsenceClaim = false,
                CompatibilityPromotion = false
            })
        );
    }
}

using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DelayBinaryOriginal411Admission
{
    internal static async Task Run()
    {
        // Separate owner/selection from the strict two-module worker consumer.
        var selection = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(DelayBinaryOriginal411Names.Runtime)
        ]);
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, CancellationToken.None);
        byte[] bytes = DelayBinaryOriginal411Access.Resource(DelayBinaryOriginal411Names.DistUtilImage);
        string[] imports = JsonSerializer.Deserialize<string[]>(DelayBinaryOriginal411Access.Resource(DelayBinaryOriginal411Names.DistUtilImports))!;
        BeamFunctionSignature[] expected = JsonSerializer.Deserialize<BeamFunctionSignature[]>(DelayBinaryOriginal411Access.Resource(DelayBinaryOriginal411Names.DistUtilExports))!;
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports));
        DelayBinaryOriginal411Access.Require(image.Name == DelayBinaryOriginal411Names.DistUtilModule
            && image.NativePrimitiveExports.Count == 0 && image.RegistryModuleDependencies.SetEquals(imports)
            && !runtime.Modules.IsModuleLoaded(image.Name));
        DelayBinaryOriginal411Access.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        var exports = runtime.Modules.Exports.Where(value => value.Module == image.Name).ToArray();
        bool owners = exports.All(value => ReferenceEquals(runtime.Modules.SourceOwner(value.Module, value.Function, value.Arity), image));
        bool exactExports = exports.Select(value => (value.Function, value.Arity)).ToHashSet()
            .SetEquals(expected.Select(value => (value.Name, value.Arity)));
        DelayBinaryOriginal411Access.Require(image.IsConsumed && owners && exports.Length > 0 && exactExports
            && runtime.NativePrimitives.Selection == RuntimeNativePrimitives.None && runtime.NativePrimitives.Bindings.Count == 0
            && runtime.LibrarySelection.Modules.Count == 1 && runtime.LibrarySelection.Modules.All(module =>
                module.Module == DelayBinaryOriginal411Names.Runtime && module.Backend == RuntimeModuleBackend.Managed));
        string output = Path.Combine(DelayBinaryOriginal411Names.ArtifactRoot, Guid.NewGuid().ToString(DelayBinaryOriginal411Names.GuidFormat));
        Directory.CreateDirectory(output);
        File.WriteAllText(
            Path.Combine(output, DelayBinaryOriginal411Names.AdmissionReceipt),
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

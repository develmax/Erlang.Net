using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class ExportGlobalOriginal431Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ExportGlobalOriginal431Names.Failure);
    }

    internal static byte[] Resource(string file)
    {
        byte[] Read(string name)
        {
            using Stream input = typeof(ExportGlobalOriginal431Access).Assembly.GetManifestResourceStream(ExportGlobalOriginal431Names.Prefix + name)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(ExportGlobalOriginal431Names.Manifest))!;
        byte[] bytes = Read(file);
        Require(hashes[file] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime(1);
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapDebugSharing425.Register(runtime, registry);
                RuntimeBootstrapHamtMap430.Register(runtime, registry);
                var declared = new BeamDependencySelection(["erlang", "prim_file", "binary", "lists", "maps", "erts_debug", "erts_internal", "export_global431_missing"]);
                PreparedBeamImage debug = registry.PrepareBeam(DebugSharingOriginal425Access.Resource("erts_debug.beam"), declared);
                PreparedBeamImage internalImage = registry.PrepareBeam(HamtOriginal430Access.Resource("erts_internal.beam"), declared);
                PreparedBeamImage worker = registry.PrepareBeam(Resource(ExportGlobalOriginal431Names.Worker), declared);
                Require(debug.NativePrimitiveExports.Count == 4 && internalImage.NativePrimitiveExports.Count == 2);
                Require(registry.PublishPreparedBeam([debug, internalImage, worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { debug, internalImage, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });

            return runtime;
        }
        catch (Exception error)
        {
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
            throw;
        }
    }
}

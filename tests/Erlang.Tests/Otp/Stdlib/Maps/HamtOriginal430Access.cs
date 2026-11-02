using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class HamtOriginal430Access
{
    internal static byte[] Resource(string file)
    {
        byte[] Read(string name)
        {
            using Stream input = typeof(HamtOriginal430Access).Assembly.GetManifestResourceStream(HamtOriginal430Names.Prefix + name)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(HamtOriginal430Names.Manifest))!;
        byte[] bytes = Read(file);
        HamtNative430Access.Require(hashes[file] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime(1);
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    RuntimeBootstrapHamtMap430.Register(runtime, registry);
                });
                var declared = new BeamDependencySelection(["erlang", "prim_file", "binary", "lists", "maps", "erts_debug", "erts_internal"]);
                PreparedBeamImage debug = scope.Prepare(() => registry.PrepareBeam(Resource("erts_debug.beam"), declared));
                PreparedBeamImage internalImage = scope.Prepare(() => registry.PrepareBeam(Resource("erts_internal.beam"), declared));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(Resource(HamtOriginal430Names.Worker), declared));
                HamtNative430Access.Require(debug.NativePrimitiveExports.Count == 2 && internalImage.NativePrimitiveExports.Count == 2);
                foreach (var signature in new[] { new BeamFunctionSignature("map_info", 1), new BeamFunctionSignature("same", 2) })
                    HamtNative430Access.Require(debug.NativePrimitiveExports.Contains(signature));
                foreach (var signature in new[] { new BeamFunctionSignature("map_hashmap_children", 1), new BeamFunctionSignature("term_type", 1) })
                    HamtNative430Access.Require(internalImage.NativePrimitiveExports.Contains(signature));

                return _ =>
                {
                    HamtNative430Access.Require(registry.PublishPreparedBeam([debug, internalImage, worker]).Status == BeamPublicationStatus.Published);
                    foreach (var image in new[] { debug, internalImage, worker })
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            HamtNative430Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                };
            }
            );

            return runtime;
        }
        catch (Exception original)
        {
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}

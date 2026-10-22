using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class DebugDispatchOriginal432Access
{
    internal static byte[] Resource(string file)
    {
        byte[] Read(string name)
        {
            using Stream input = typeof(DebugDispatchOriginal432Access).Assembly.GetManifestResourceStream(DebugDispatchOriginal432Names.Prefix + name)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read("manifest.json"))!;
        byte[] bytes = Read(file);
        DebugDispatch432Require(hashes[file] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static void DebugDispatch432Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugDispatch432Names.Failure);
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime(1);
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapDebugDispatch432.Register(runtime, registry);
                var selection = new BeamDependencySelection(["erlang", "prim_file", "erts_debug", DebugDispatchOriginal432Names.Module]);
                PreparedBeamImage facet = registry.PrepareBeam(Resource("erts_debug.beam"), selection);
                PreparedBeamImage worker = registry.PrepareBeam(Resource(DebugDispatchOriginal432Names.Worker), selection);
                DebugDispatch432Require(facet.NativePrimitiveExports.Count == 2);
                DebugDispatch432Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (var image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DebugDispatch432Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
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

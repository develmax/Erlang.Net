using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class AtomicsHost413Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(AtomicsHost413Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(AtomicsHost413Access).Assembly.GetManifestResourceStream(AtomicsHost413Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(AtomicsHost413Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime();
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    RuntimeBootstrapAtomics.Register(runtime, registry);
                });
                var dependencies = new BeamDependencySelection([AtomicsHost413Names.Runtime,AtomicsHost413Names.Internal,
                                AtomicsHost413Names.Atomics,AtomicsHost413Names.Lists,AtomicsHost413Names.Maps]);
                var images = new[] { AtomicsHost413Names.Atomics, AtomicsHost413Names.Lists, AtomicsHost413Names.Maps }
                    .Select(module => scope.Prepare(() => registry.PrepareBeam(Resource(module + AtomicsHost413Names.Beam), dependencies))).ToArray();
                PreparedBeamImage atomics = images[0];
                Require(atomics.NativePrimitiveExports.Count == AtomicsHost413Names.Natives);

                return _ =>
                {
                    Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    var exports = registry.Exports.Where(export => export.Module == AtomicsHost413Names.Atomics).ToArray();
                    Require(exports.Length == AtomicsHost413Names.Exports);
                    foreach (var export in exports)
                        Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), atomics));
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

    internal static Task<Term> Call(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string function,
        params Term[] arguments
    )
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            AtomicsHost413Names.Atomics,
            function,
            arguments
        ));
}

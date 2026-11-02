using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class NodeNative501Checks
{
    internal static Task Run((string Family, string Subject, int Values, int Profiles, bool Atomics, string ManifestHash) group)
    {
        string prefix = NodeNative501Names.Prefix + group.Family + NodeNative501Names.Separator;
        byte[] manifest = Resource(prefix, NodeNative501Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != group.ManifestHash)
            throw new InvalidDataException(NodeNative501Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(NodeNative501Names.Failure);

        byte[] Read(string name)
        {
            if (name != group.Subject + NodeNative501Names.BeamExtension
                && name is not (NodeNative501Names.ValuesFile or NodeNative501Names.StacksFile))
                return BeamOrdsetsCoreHostChecks.Read(name);
            byte[] bytes = Resource(prefix, name);
            if (!digests.TryGetValue(name, out string? expected)
                || Convert.ToHexString(SHA256.HashData(bytes)) != expected)
                throw new InvalidDataException(NodeNative501Names.Failure);

            return bytes;
        }

        return BeamOrdsetsCoreHostChecks.RunSubject(
            group.Subject,
            Read,
            group.Values,
            group.Profiles,
            group.Atomics ? InitializeAtomics : null,
            encodeInRuntime: true,
            additionalDependencies: [AtomicsHost413Names.Atomics, AtomicsHost413Names.Internal]
        );
    }

    private static async Task InitializeAtomics(ProcessRuntime runtime)
        => await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
           {
               await runtime.InitializeModulesAsync(_ =>
               {
                   RuntimeBootstrapAtomics.Register(runtime, registry);
               });
               PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(
                   AtomicsHost413Access.Resource(AtomicsHost413Names.Atomics + AtomicsHost413Names.Beam),
                   new BeamDependencySelection([AtomicsHost413Names.Runtime, AtomicsHost413Names.Internal])
               ));

               return _ =>
               {
                   if (registry.PublishPreparedBeam([image]).Status != BeamPublicationStatus.Published)
                       throw new InvalidDataException(NodeNative501Names.Failure);
               };
           }
        );

    private static byte[] Resource(string prefix, string name)
    {
        using Stream input = typeof(NodeNative501Checks).Assembly.GetManifestResourceStream(prefix + name)
            ?? throw new InvalidDataException(NodeNative501Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

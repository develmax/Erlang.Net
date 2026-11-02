using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class MapContract495Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(MapContract495Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != MapContract495Names.ManifestSha256)
            throw new InvalidDataException(MapContract495Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(MapContract495Names.Failure);

        return BeamOrdsetsCoreHostChecks.RunSubject(
            group.Subject,
            name => Read(
                name,
                group.Group,
                group.Subject,
                digests
            ),
            group.Values,
            group.Profiles,
            Initialize,
            encodeInRuntime: true,
            additionalDependencies: [MapContract495Names.Records, MapContract495Names.Internal, MapContract495Names.Debug]
        );
    }

    private static async Task Initialize(ProcessRuntime runtime)
        => await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
           {
               await runtime.InitializeModulesAsync(_ =>
               {
                   RuntimeBootstrapHamtMap430.Register(runtime, registry);
               });
               var dependencies = new BeamDependencySelection([MapContract495Names.Erlang,
                           MapContract495Names.PrimFile, MapContract495Names.Binary, MapContract495Names.Lists,
                           MapContract495Names.Maps, MapContract495Names.Debug, MapContract495Names.Internal]);
               PreparedBeamImage[] images = [
                   scope.Prepare(() => registry.PrepareBeam(HamtOriginal430Access.Resource(MapContract495Names.Debug + MapContract495Names.BeamExtension), dependencies)),
                           scope.Prepare(() => registry.PrepareBeam(HamtOriginal430Access.Resource(MapContract495Names.Internal + MapContract495Names.BeamExtension), dependencies))
               ];

               return _ =>
               {
                   if (images.Any(image => image.NativePrimitiveExports.Count != 2)
                   || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                       throw new InvalidDataException(MapContract495Names.Failure);
               };
           }
        );

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name is MapContract495Names.ValuesFile or MapContract495Names.StacksFile
            ? group + MapContract495Names.Separator + name : name;
        if (name != subject + MapContract495Names.BeamExtension
            && name is not (MapContract495Names.ValuesFile or MapContract495Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(MapContract495Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(MapContract495Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(MapContract495Checks).Assembly.GetManifestResourceStream(MapContract495Names.Prefix + name)
            ?? throw new InvalidDataException(MapContract495Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

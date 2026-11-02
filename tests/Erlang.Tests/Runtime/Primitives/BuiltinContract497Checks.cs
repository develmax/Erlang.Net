using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class BuiltinContract497Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(BuiltinContract497Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != BuiltinContract497Names.ManifestSha256)
            throw new InvalidDataException(BuiltinContract497Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(BuiltinContract497Names.Failure);

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
            additionalDependencies: [BuiltinContract497Names.Records, BuiltinContract497Names.Internal, BuiltinContract497Names.Debug, BuiltinContract497Names.Maps]
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
               var dependencies = new BeamDependencySelection([BuiltinContract497Names.Erlang,
                           BuiltinContract497Names.PrimFile, BuiltinContract497Names.Binary, BuiltinContract497Names.Lists,
                           BuiltinContract497Names.Maps, BuiltinContract497Names.Debug, BuiltinContract497Names.Internal]);
               PreparedBeamImage[] images = [
                   scope.Prepare(() => registry.PrepareBeam(HamtOriginal430Access.Resource(BuiltinContract497Names.Debug + BuiltinContract497Names.BeamExtension), dependencies)),
                           scope.Prepare(() => registry.PrepareBeam(HamtOriginal430Access.Resource(BuiltinContract497Names.Internal + BuiltinContract497Names.BeamExtension), dependencies))
               ];

               return _ =>
               {
                   if (images.Any(image => image.NativePrimitiveExports.Count != 2)
                   || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                       throw new InvalidDataException(BuiltinContract497Names.Failure);
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
        string file = name is BuiltinContract497Names.ValuesFile or BuiltinContract497Names.StacksFile
            ? group + BuiltinContract497Names.Separator + name : name;
        if (name != subject + BuiltinContract497Names.BeamExtension
            && name is not (BuiltinContract497Names.ValuesFile or BuiltinContract497Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(BuiltinContract497Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(BuiltinContract497Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(BuiltinContract497Checks).Assembly.GetManifestResourceStream(BuiltinContract497Names.Prefix + name)
            ?? throw new InvalidDataException(BuiltinContract497Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

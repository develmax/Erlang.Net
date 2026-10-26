using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RecordContract496Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(RecordContract496Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != RecordContract496Names.ManifestSha256)
            throw new InvalidDataException(RecordContract496Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(RecordContract496Names.Failure);

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
            additionalDependencies: [RecordContract496Names.Records, RecordContract496Names.Internal, RecordContract496Names.Debug, RecordContract496Names.Maps]
        );
    }

    private static async Task Initialize(ProcessRuntime runtime)
        => await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapHamtMap430.Register(runtime, registry);
            var dependencies = new BeamDependencySelection([RecordContract496Names.Erlang,
                RecordContract496Names.PrimFile, RecordContract496Names.Binary, RecordContract496Names.Lists,
                RecordContract496Names.Maps, RecordContract496Names.Debug, RecordContract496Names.Internal]);
            PreparedBeamImage[] images = [
                registry.PrepareBeam(HamtOriginal430Access.Resource(RecordContract496Names.Debug + RecordContract496Names.BeamExtension), dependencies),
                registry.PrepareBeam(HamtOriginal430Access.Resource(RecordContract496Names.Internal + RecordContract496Names.BeamExtension), dependencies)
            ];
            if (images.Any(image => image.NativePrimitiveExports.Count != 2)
                || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                throw new InvalidDataException(RecordContract496Names.Failure);
        });

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name is RecordContract496Names.ValuesFile or RecordContract496Names.StacksFile
            ? group + RecordContract496Names.Separator + name : name;
        if (name != subject + RecordContract496Names.BeamExtension
            && name is not (RecordContract496Names.ValuesFile or RecordContract496Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(RecordContract496Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(RecordContract496Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(RecordContract496Checks).Assembly.GetManifestResourceStream(RecordContract496Names.Prefix + name)
            ?? throw new InvalidDataException(RecordContract496Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

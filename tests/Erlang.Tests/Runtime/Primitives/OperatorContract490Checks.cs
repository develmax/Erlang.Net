using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class OperatorContract490Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(OperatorContract490Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != OperatorContract490Names.ManifestSha256)
            throw new InvalidDataException(OperatorContract490Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(OperatorContract490Names.Failure);

        return BeamOrdsetsCoreHostChecks.RunSubject(
            group.Subject,
            name => Read(
                name,
                group.Group,
                group.Subject,
                digests
            ),
            group.Values,
            group.Profiles
        );
    }

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name is OperatorContract490Names.ValuesFile or OperatorContract490Names.StacksFile
            ? group + OperatorContract490Names.Separator + name : name;
        if (name != subject + OperatorContract490Names.BeamExtension
            && name is not (OperatorContract490Names.ValuesFile or OperatorContract490Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(OperatorContract490Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(OperatorContract490Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(OperatorContract490Checks).Assembly.GetManifestResourceStream(OperatorContract490Names.Prefix + name)
            ?? throw new InvalidDataException(OperatorContract490Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

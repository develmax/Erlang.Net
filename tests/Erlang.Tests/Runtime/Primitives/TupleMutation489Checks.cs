using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class TupleMutation489Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(TupleMutation489Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != TupleMutation489Names.ManifestSha256)
            throw new InvalidDataException(TupleMutation489Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(TupleMutation489Names.Failure);

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
        string file = name is TupleMutation489Names.ValuesFile or TupleMutation489Names.StacksFile
            ? group + TupleMutation489Names.Separator + name : name;
        if (name != subject + TupleMutation489Names.BeamExtension
            && name is not (TupleMutation489Names.ValuesFile or TupleMutation489Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(TupleMutation489Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(TupleMutation489Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(TupleMutation489Checks).Assembly.GetManifestResourceStream(TupleMutation489Names.Prefix + name)
            ?? throw new InvalidDataException(TupleMutation489Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

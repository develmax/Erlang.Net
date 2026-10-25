using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class IntegerContract493Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(IntegerContract493Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != IntegerContract493Names.ManifestSha256)
            throw new InvalidDataException(IntegerContract493Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(IntegerContract493Names.Failure);

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
        string file = name is IntegerContract493Names.ValuesFile or IntegerContract493Names.StacksFile
            ? group + IntegerContract493Names.Separator + name : name;
        if (name != subject + IntegerContract493Names.BeamExtension
            && name is not (IntegerContract493Names.ValuesFile or IntegerContract493Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(IntegerContract493Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(IntegerContract493Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(IntegerContract493Checks).Assembly.GetManifestResourceStream(IntegerContract493Names.Prefix + name)
            ?? throw new InvalidDataException(IntegerContract493Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

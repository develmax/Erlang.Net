using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ArithmeticContract491Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(ArithmeticContract491Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != ArithmeticContract491Names.ManifestSha256)
            throw new InvalidDataException(ArithmeticContract491Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(ArithmeticContract491Names.Failure);

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
            DebugSame420HostAccess.Initialize
        );
    }

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name is ArithmeticContract491Names.ValuesFile or ArithmeticContract491Names.StacksFile
            ? group + ArithmeticContract491Names.Separator + name : name;
        if (name != subject + ArithmeticContract491Names.BeamExtension
            && name is not (ArithmeticContract491Names.ValuesFile or ArithmeticContract491Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(ArithmeticContract491Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(ArithmeticContract491Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(ArithmeticContract491Checks).Assembly.GetManifestResourceStream(ArithmeticContract491Names.Prefix + name)
            ?? throw new InvalidDataException(ArithmeticContract491Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

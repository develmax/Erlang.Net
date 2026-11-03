using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class AtomContract492Checks
{
    internal static async Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(AtomContract492Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != AtomContract492Names.ManifestSha256)
            throw new InvalidDataException(AtomContract492Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(AtomContract492Names.Failure);

        await BeamOrdsetsCoreHostChecks.RunSubject(
            group.Subject,
            name => Read(
                name,
                group.Group,
                group.Subject,
                digests
            ),
            group.Values,
            group.Profiles,
            group.Group == AtomContract492Names.IdentityGroup ? DebugSame420HostAccess.Initialize : null
        );
        if (group.Group == AtomContract492Names.IdentityGroup)
            await VmConstantPool357Checks.Run();
    }

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name is AtomContract492Names.ValuesFile or AtomContract492Names.StacksFile
            ? group + AtomContract492Names.Separator + name : name;
        if (name != subject + AtomContract492Names.BeamExtension
            && name is not (AtomContract492Names.ValuesFile or AtomContract492Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(AtomContract492Names.Failure);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(AtomContract492Names.Failure);

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(AtomContract492Checks).Assembly.GetManifestResourceStream(AtomContract492Names.Prefix + name)
            ?? throw new InvalidDataException(AtomContract492Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

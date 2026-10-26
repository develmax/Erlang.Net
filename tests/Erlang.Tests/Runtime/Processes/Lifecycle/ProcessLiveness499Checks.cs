using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ProcessLiveness499Checks
{
    internal static Task Run((string Group, string Subject, int Values, int Profiles) group)
    {
        byte[] manifest = Resource(ProcessLiveness499Names.Manifest);
        if (Convert.ToHexString(SHA256.HashData(manifest)) != ProcessLiveness499Names.ManifestSha256)
            throw new InvalidDataException(ProcessLiveness499Names.Failure);
        var digests = JsonSerializer.Deserialize<Dictionary<string, string>>(manifest)
            ?? throw new InvalidDataException(ProcessLiveness499Names.Failure);

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
            async runtime => await runtime.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.PersistentTermCore)),
            encodeInRuntime: true,
            additionalDependencies: [ProcessLiveness499Names.PersistentTerm]
        );
    }

    private static byte[] Read(
        string name,
        string group,
        string subject,
        IReadOnlyDictionary<string, string> digests
    )
    {
        string file = name;
        if (name != subject + ProcessLiveness499Names.BeamExtension
            && name is not (ProcessLiveness499Names.ValuesFile or ProcessLiveness499Names.StacksFile))
            return BeamOrdsetsCoreHostChecks.Read(name);

        if (!digests.TryGetValue(file, out string? expected))
            throw new InvalidDataException(ProcessLiveness499Names.Failure + file);
        byte[] bytes = Resource(file);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(ProcessLiveness499Names.Failure + file + Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(ProcessLiveness499Checks).Assembly.GetManifestResourceStream(ProcessLiveness499Names.Prefix + name)
            ?? throw new InvalidDataException(ProcessLiveness499Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}

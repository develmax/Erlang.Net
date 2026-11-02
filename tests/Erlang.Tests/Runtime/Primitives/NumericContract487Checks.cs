using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class NumericContract487Checks
{
    internal static async Task Run()
    {
        await BeamOrdsetsCoreHostChecks.RunSubject(
            NumericContract487Names.Subject,
            Read,
            NumericContract487Names.Values,
            NumericContract487Names.Profiles,
            DebugSame420HostAccess.Initialize
        );
        await InlineInteger360Checks.Run();
    }

    private static byte[] Read(string name)
    {
        if (!NumericContract487Names.Digests.TryGetValue(name, out string? expected))
            return BeamOrdsetsCoreHostChecks.Read(name);

        using Stream input = typeof(NumericContract487Checks).Assembly.GetManifestResourceStream(NumericContract487Names.Prefix + name)
            ?? throw new InvalidDataException(NumericContract487Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(NumericContract487Names.Failure);

        return bytes;
    }
}

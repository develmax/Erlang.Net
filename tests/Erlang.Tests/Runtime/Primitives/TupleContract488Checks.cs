using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class TupleContract488Checks
{
    internal static Task Run() => BeamOrdsetsCoreHostChecks.RunSubject(
        TupleContract488Names.Subject,
        Read,
        TupleContract488Names.Values,
        TupleContract488Names.Profiles,
        DebugSame420HostAccess.Initialize
    );

    private static byte[] Read(string name)
    {
        if (!TupleContract488Names.Digests.TryGetValue(name, out string? expected))
            return BeamOrdsetsCoreHostChecks.Read(name);

        using Stream input = typeof(TupleContract488Checks).Assembly.GetManifestResourceStream(TupleContract488Names.Prefix + name)
            ?? throw new InvalidDataException(TupleContract488Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(TupleContract488Names.Failure);

        return bytes;
    }
}

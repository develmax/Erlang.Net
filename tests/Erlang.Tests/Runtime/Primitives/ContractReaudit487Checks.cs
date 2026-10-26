using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class ContractReaudit487Checks
{
    internal static Task Run() => BeamOrdsetsCoreHostChecks.RunSubject(
        ContractReaudit487Names.Subject,
        Read,
        ContractReaudit487Names.Values,
        ContractReaudit487Names.Profiles
    );

    private static byte[] Read(string name)
    {
        if (!ContractReaudit487Names.Digests.TryGetValue(name, out string? expected))
            return BeamOrdsetsCoreHostChecks.Read(name);

        using Stream input = typeof(ContractReaudit487Checks).Assembly.GetManifestResourceStream(ContractReaudit487Names.Prefix + name)
            ?? throw new InvalidDataException(ContractReaudit487Names.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);

        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(ContractReaudit487Names.Failure);

        return bytes;
    }
}

using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class LibrarySelectionHostData
{
    internal static byte[] Lists() => Read(ListBeamHostNames.Resource, ListBeamHostNames.Sha256);

    internal static byte[] Queue() => Read(QueueBeamAdmissionHostNames.Resource, QueueBeamAdmissionHostNames.Sha256);

    internal static RuntimeLibrarySelection Both(bool reverse = false)
    {
        RuntimeModuleSelection[] choices =
        [
            RuntimeModuleSelection.Beam(ListBeamHostNames.Module, Lists()),
            RuntimeModuleSelection.Beam(QueueModuleNames.Module, Queue(), [ListBeamHostNames.Module])
        ];

        return new(reverse ? choices.Reverse() : choices);
    }

    internal static byte[] Read(string suffix, string expectedHash)
    {
        Assembly assembly = typeof(LibrarySelectionHostData).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == expectedHash);

        return bytes;
    }

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(LibrarySelectionHostNames.Failure);
    }
}

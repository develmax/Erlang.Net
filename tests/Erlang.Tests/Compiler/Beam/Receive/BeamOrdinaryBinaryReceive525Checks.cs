using System.Security.Cryptography;
using static Erlang.Tests.BeamOrdinaryBinaryReceive525Names;

namespace Erlang.Tests;

internal static class BeamOrdinaryBinaryReceive525Checks
{
    internal static async Task OriginalWholeImage()
    {
        var assembly = typeof(BeamOrdinaryBinaryReceive525Checks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ResourceSeparator + Resource, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var storage = new MemoryStream();
        input.CopyTo(storage);
        byte[] bytes = storage.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != ExpectedSha256)
            throw new InvalidOperationException(Failure);
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(Dependencies));
        if (prepared.Name != GuestModule || prepared.OriginalBeamSha256 != ExpectedSha256 || prepared.HasOnLoad)
            throw new InvalidOperationException(Failure);
    }
}

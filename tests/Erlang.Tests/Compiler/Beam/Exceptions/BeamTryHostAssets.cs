using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamTryHostAssets
{
    internal static PreparedBeamImage Select(ProcessRuntime runtime)
    {
        byte[] bytes = Read(BeamTryHostNames.GuestResource, BeamTryHostNames.GuestSha256);
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        if (image.Name != BeamTryHostNames.Module
            || runtime.Modules.PublishPreparedBeam(image).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);
        Verify(runtime, image);

        return image;
    }

    internal static TupleTerm Expected(string name)
    {
        Term decoded = ExternalTermFormat.Decode(Read(BeamTryHostNames.OriginalResource, BeamTryHostNames.OriginalSha256));
        Term[] values = Cons.Items(decoded).ToArray();
        if (values.Length != BeamTryHostNames.CaseCount)
            throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);
        var rows = new List<TupleTerm>();
        for (int index = 0; index < values.Length; index++)
        {
            if (values[index] is not TupleTerm { Items.Count: 2 } row || row.Items[0] is not Atom atom
                || atom.Name != BeamTryHostCaseNames.Ordered[index])
                throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);
            rows.Add(row);
        }

        return rows.Single(row => ((Atom)row.Items[0]).Name == name);
    }

    internal static void Verify(ProcessRuntime runtime, PreparedBeamImage image)
    {
        var exports = runtime.Modules.Exports.Where(entry => entry.Module == BeamTryHostNames.Module).ToArray();
        if (!exports.Any(entry => entry.Function == BeamTryHostNames.Cases && entry.Arity == 0)
            || !exports.Any(entry => entry.Function == BeamTryHostNames.Capture && entry.Arity == 2)
            || exports.Any(entry => !ReferenceEquals(runtime.Modules.SourceOwner(
                entry.Module,
                entry.Function,
                entry.Arity
            ), image)))
            throw new InvalidDataException(BeamTryHostNames.OwnerFailure);
    }

    private static byte[] Read(string name, string expectedSha)
    {
        using Stream stream = typeof(BeamTryHostAssets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expectedSha)
            throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);

        return bytes;
    }
}

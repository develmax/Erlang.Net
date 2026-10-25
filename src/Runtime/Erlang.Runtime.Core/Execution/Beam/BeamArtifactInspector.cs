using System.Security.Cryptography;

namespace Erlang;

public static class BeamArtifactInspector
{
    public static BeamArtifactInfo Inspect(ReadOnlyMemory<byte> bytes)
    {
        byte[] owned = bytes.ToArray();
        var exports = new RuntimeFunctionEntries();
        var decoding = new EtfDecodeOptions
        {
            ExternalFunctionFactory = (identity, safe) => safe
                ? throw new InvalidOperationException(BeamArtifactInspectionNames.UnexpectedSafeMode)
                : exports.ExternalForArtifactInspection(identity)
        };
        BeamLinkedCode linked = BeamLinkedCodeReader.Read(BeamStructureReader.Read(owned, decoding));

        return new(
            linked.Structure.Atoms[1]!,
            linked.OnLoadEntryLabel is not null,
            Convert.ToHexString(SHA256.HashData(owned))
        );
    }
}

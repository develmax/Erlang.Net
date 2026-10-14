namespace Erlang;

internal static class BeamLambdaReader
{
    internal static IReadOnlyList<BeamLambdaEntry> Read(BeamStructure structure)
    {
        BeamChunk? chunk = structure.Chunks.LastOrDefault(value => value.Id == BeamLambdaWire.Chunk);
        if (chunk is null || chunk.Payload.Length == 0)
        {
            return Array.Empty<BeamLambdaEntry>();
        }
        var cursor = new BeamCursor(chunk.Payload);
        int count = cursor.ReadInt32();
        Require(count >= 0
            && count < BeamLambdaWire.NativeAllocationSanityBytes / BeamLambdaWire.NativeEntryBytes64
            && count <= cursor.Remaining / BeamLambdaWire.EntryBytes);
        var entries = new BeamLambdaEntry[count];
        for (int index = 0; index < count; index++)
        {
            int atom = cursor.ReadInt32();
            int arity = cursor.ReadInt32();
            int label = cursor.ReadInt32();
            int storedIndex = cursor.ReadInt32();
            int free = cursor.ReadInt32();
            int uniqueness = cursor.ReadInt32();
            Require(atom >= 0 && atom < structure.Atoms.Count
                && label > 0 && label < structure.Code.LabelCount);
            entries[index] = new(
                atom,
                arity,
                label,
                storedIndex,
                free,
                uniqueness
            );
        }

        return Array.AsReadOnly(entries);
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidDataException(BeamLambdaDiagnostics.Table);
        }
    }
}

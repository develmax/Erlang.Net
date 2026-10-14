using System.IO.Compression;

namespace Erlang;

internal static class BeamLiteralReader
{
    public static IReadOnlyList<Term> Read(ReadOnlyMemory<byte> payload)
    {
        var header = new BeamCursor(payload);
        int capacity = header.ReadInt32();
        if (capacity < 0 || capacity > BeamLiteralLimits.MaximumDecodedTableBytes)
            throw new InvalidDataException(BeamLiteralDiagnostics.InvalidCapacity);
        ReadOnlyMemory<byte> data = header.Read(header.Remaining);
        if (capacity != BeamLiteralWire.UncompressedTableMarker)
            data = Decompress(data, capacity);
        if (data.Length > BeamLiteralLimits.MaximumDecodedTableBytes)
            throw new InvalidDataException(BeamLiteralDiagnostics.InvalidCapacity);
        var cursor = new BeamCursor(data);
        int count = cursor.ReadInt32();
        if (count < 0 || (long)count * BeamLiteralWire.LiteralEntryBytes >= BeamWire.TableAllocationSanityBytes
            || count > cursor.Remaining / BeamLiteralWire.EntrySizeFieldBytes)
            throw new InvalidDataException(BeamLiteralDiagnostics.InvalidCount);
        var literals = new List<Term>(count);
        for (int index = 0; index < count; index++)
        {
            ReadOnlyMemory<byte> external = cursor.Read(cursor.ReadInt32());
            if (external.Length > BeamLiteralWire.ExternalTermTagOffset && external.Span[BeamLiteralWire.ExternalTermTagOffset] == BeamLiteralWire.CompressedExternalTermTag)
                throw new InvalidDataException(BeamLiteralDiagnostics.InvalidExternalTerm);
            try
            {
                literals.Add(ExternalTermFormat.DecodeOne(external.Span, new EtfDecodeOptions(), BeamLiteralLimits.MaximumDecodedTableBytes).Term);
            }
            catch (ErlangException exception)
            {
                throw new InvalidDataException(BeamLiteralDiagnostics.InvalidExternalTerm, exception);
            }
        }

        return literals.AsReadOnly();
    }

    private static ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressed, int capacity)
    {
        try
        {
            int memberBytes = BeamZlibMemberBoundary.Measure(compressed.Span, capacity);
            using var input = new MemoryStream(compressed[..memberBytes].ToArray(), false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buffer = new byte[BeamLiteralLimits.DecompressionBufferBytes];
            int read;
            while ((read = zlib.Read(buffer)) != 0)
            {
                if (output.Length + read > capacity)
                    throw new InvalidDataException(BeamLiteralDiagnostics.InvalidCapacity);
                output.Write(buffer, 0, read);
            }

            return output.ToArray();
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException(BeamLiteralDiagnostics.InvalidCompressedTable, exception);
        }
    }
}

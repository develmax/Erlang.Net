using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Erlang;

internal sealed partial class BeamfileMd5Document423
{
    private void ParseLiterals()
    {
        if (!Optional(BeamfileMd5Names423.Literals, out var payload)) return;
        var cursor = new BeamCursor(payload);
        int capacity = cursor.ReadInt32();
        Require(capacity >= 0);
        ReadOnlyMemory<byte> data = cursor.Read(cursor.Remaining);
        if (capacity != 0)
        {
            int member = BeamZlibMemberBoundary.Measure(data.Span, capacity);
            using var input = new MemoryStream(data[..member].ToArray(), false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buffer = new byte[BeamLiteralLimits.DecompressionBufferBytes];
            int read;
            while ((read = zlib.Read(buffer)) != 0)
            {
                cancellation.ThrowIfCancellationRequested();
                Require(output.Length + read <= capacity);
                output.Write(buffer, 0, read);
            }
            data = output.ToArray();
        }
        var body = new BeamCursor(data);
        int count = Count(body, 0, BeamfileMd5Names423.LiteralEntrySize);
        var values = new List<Term>();
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            ReadOnlyMemory<byte> external = body.Read(body.ReadInt32());
            Require(external.Length >= BeamLiteralWire.ExternalTermTagOffset + 1
                && external.Span[BeamLiteralWire.ExternalTermTagOffset] != BeamLiteralWire.CompressedExternalTermTag);
            // Both native erts_decode_ext_size and distribution sizing call
            // decoded_size(..., internal_tags=0, ctx=NULL). Reuse that exact
            // flat state pass and decoder atom-field protocol, with NO atom
            // translation cache. Own atom indices remain the real registry.
            values.Add(runtime.ImportExternalTerm(
                external.Span,
                new EtfDecodeOptions
                {
                    DistributionDebug423 = true,
                    LocalFunctionFactory = (metadata, arity, environment) => runtime.Functions.CreateStoredRestored(
                        runtime,
                        new LocalFunctionIdentity(
                            metadata.Module,
                            null,
                            metadata.Index,
                            metadata.OldUnique,
                            metadata.Unique,
                            metadata.OldIndex
                        ),
                        arity,
                        environment
                    )
                },
                int.MaxValue
            ).Term);
        }
        literals = values.AsReadOnly();
    }

    private void ParseLines()
    {
        if (!Optional(BeamfileMd5Names423.Lines, out var payload)) return;
        var cursor = new BeamCursor(payload);
        if (cursor.ReadInt32() != 0) return;
        cursor.ReadInt32(); // flags are data, not a validity predicate
        cursor.ReadInt32(); // instruction count is retained by OTP without validation
        int items = Count(cursor, 0, BeamfileMd5Names423.LineEntrySize);
        int names = Count(cursor, 0, BeamfileMd5Names423.NameEntrySize);
        int completed = 0;
        while (completed < items)
        {
            cancellation.ThrowIfCancellationRequested();
            BeamCompactValue value = BeamCompactReader.Read(cursor);
            switch (value.WireTag)
            {
                case BeamOperandParameters.AtomTag:
                    // Pinned parse_line_chunk compares against name_count
                    // after adding its implicit module-name slot.
                    Require(value.FitsNativeWord && value.Value >= 0 && value.Value <= names + 1);
                    break;
                case BeamWire.IntegerTag:
                    Require(value.FitsNativeWord && value.Value >= 0);
                    completed++;
                    break;
                default:
                    throw new InvalidDataException(BeamfileMd5Names423.Invalid);
            }
        }
        for (int index = 0; index < names; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            int length = BinaryPrimitives.ReadInt16BigEndian(cursor.Read(sizeof(short)).Span);
            Require(length >= 0);
            try
            {
                _ = new UTF8Encoding(false, true).GetCharCount(cursor.Read(length).Span);
            }
            catch (DecoderFallbackException error)
            {
                throw new InvalidDataException(BeamfileMd5Names423.Invalid, error);
            }
        }
        // Native reader deliberately leaves trailing bytes unconsumed.
    }
}

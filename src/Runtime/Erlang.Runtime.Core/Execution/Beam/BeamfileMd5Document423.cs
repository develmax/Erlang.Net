using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Erlang;

internal sealed partial class BeamfileMd5Document423
{
    private readonly ProcessRuntime runtime;
    private readonly CancellationToken cancellation;

    internal BeamfileMd5Document423(ProcessRuntime runtime, CancellationToken cancellation)
    {
        this.runtime = runtime;
        this.cancellation = cancellation;
    }

    private readonly Dictionary<string, ReadOnlyMemory<byte>> chunks = new(StringComparer.Ordinal);
    private readonly List<string?> atoms = [null];
    private IReadOnlyList<Term> literals = Array.Empty<Term>();
    private BeamCodeHeader code = null!;
    private BeamTypeTable types = null!;

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(BeamfileMd5Names423.Invalid);
    }

    private static int Count(BeamCursor cursor, int minimum, int nativeEntryBytes)
    {
        int value = cursor.ReadInt32();
        Require(value >= minimum && value < BeamWire.TableAllocationSanityBytes / nativeEntryBytes);

        return value;
    }

    private ReadOnlyMemory<byte> Required(string name)
    {
        Require(chunks.TryGetValue(name, out ReadOnlyMemory<byte> value) && value.Length != 0);

        return value;
    }

    private bool Optional(string name, out ReadOnlyMemory<byte> value)
        => chunks.TryGetValue(name, out value) && value.Length != 0;

    private BeamStructure Structure() => new(
        chunks.Select(pair => new BeamChunk(
            pair.Key,
            0,
            pair.Value,
            0
        )).ToArray(),
        atoms,
        Array.Empty<BeamImport>(),
        Array.Empty<BeamExport>(),
        code,
        literals,
        0
    );

    internal BitString Read(ReadOnlyMemory<byte> bytes)
    {
        ParseIff(bytes);
        ParseCode();
        ParseAtoms();
        ParseRows(BeamfileMd5Names423.Imports, true);
        ParseRows(BeamfileMd5Names423.Exports, false);
        ParseLambdas();
        ParseLiterals();
        ParseLines();
        types = BeamTypeTableReader.Read(Structure());
        ParseDebug();
        ParseRecords();
        cancellation.ThrowIfCancellationRequested();

        return Hash();
    }

    private void ParseIff(ReadOnlyMemory<byte> bytes)
    {
        var cursor = new BeamCursor(bytes);
        Require(BinaryPrimitives.ReadUInt32BigEndian(cursor.Read(BeamfileMd5Names423.Word).Span) == BeamfileNativeNames418.FormId);
        var form = new BeamCursor(cursor.Read(cursor.ReadInt32()));
        Require(BinaryPrimitives.ReadUInt32BigEndian(form.Read(BeamfileMd5Names423.Word).Span) == BeamfileNativeNames418.BeamId);
        while (form.Remaining != 0)
        {
            cancellation.ThrowIfCancellationRequested();
            string name = Encoding.Latin1.GetString(form.Read(BeamfileMd5Names423.Word).Span);
            int length = form.ReadInt32();
            Require(length >= 0);
            ReadOnlyMemory<byte> payload = form.Read(length);
            int padding = (int)((-(long)length) & BeamfileMd5Names423.AlignmentMask);
            form.Read(padding);
            chunks[name] = payload;
        }
    }

    private void ParseCode()
    {
        var cursor = new BeamCursor(Required(BeamfileMd5Names423.Code));
        var header = new BeamCursor(cursor.Read(cursor.ReadInt32()));
        int version = header.ReadInt32();
        Require(version == BeamWire.FormatVersion);
        int maximumOpcode = header.ReadInt32();
        int labels = header.ReadInt32();
        int functions = header.ReadInt32();
        code = new(
            version,
            maximumOpcode,
            labels,
            functions,
            cursor.Read(cursor.Remaining)
        );
    }

    private void ParseAtoms()
    {
        var cursor = new BeamCursor(Required(BeamfileMd5Names423.Atoms));
        int count = cursor.ReadInt32();
        bool longCounts = count < 0;
        Require(count != int.MinValue);
        count = Math.Abs(count);
        Require(count >= 1 && count < BeamWire.TableAllocationSanityBytes / BeamfileMd5Names423.AtomEntrySize);
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            int length;
            if (longCounts)
            {
                BeamCompactValue value = BeamCompactReader.Read(cursor);
                Require(value.FitsNativeWord && value.Value >= 0 && value.Value <= int.MaxValue);
                length = (int)value.Value;
            }
            else length = cursor.ReadByte();
            ReadOnlySpan<byte> text = cursor.Read(length).Span;
            var result = new StringBuilder();
            for (int characters = 0; !text.IsEmpty && characters < BeamWire.MaximumAtomCharacters; characters++)
            {
                Require(Rune.DecodeFromUtf8(text, out Rune rune, out int used) == OperationStatus.Done);
                result.Append(rune.ToString());
                text = text[used..];
            }
            string atom = result.ToString();
            AtomNameRegistry.IndexOf(atom);
            atoms.Add(atom);
        }
    }

    private void ParseRows(string name, bool imports)
    {
        var cursor = new BeamCursor(Required(name));
        int count = Count(cursor, 0, imports ? BeamfileMd5Names423.ImportEntrySize : BeamfileMd5Names423.ExportEntrySize);
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            int atom = cursor.ReadInt32(), second = cursor.ReadInt32(), third = cursor.ReadInt32();
            Require(atom >= 0 && atom < atoms.Count);
            if (imports) Require(second >= 0 && second < atoms.Count && third >= 0 && third <= BeamWire.MaximumArity);
            else Require(second >= 0 && second <= BeamWire.MaximumArity && third >= 0);
        }
    }

    private void ParseLambdas()
    {
        if (!Optional(BeamfileMd5Names423.Lambdas, out var payload)) return;
        var cursor = new BeamCursor(payload);
        int count = Count(cursor, 0, BeamfileMd5Names423.LambdaEntrySize);
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            int atom = cursor.ReadInt32();
            cursor.ReadInt32();
            int label = cursor.ReadInt32();
            cursor.ReadInt32();
            cursor.ReadInt32();
            cursor.ReadInt32();
            Require(atom >= 0 && atom < atoms.Count && label > 0 && (ulong)label < unchecked((ulong)(long)code.LabelCount));
        }
        // Checksum normalizes OldUniq in every physical 24-byte entry,
        // including any complete trailing entries not counted by the header.
        Require((payload.Length - BeamfileMd5Names423.Word) % BeamfileMd5Names423.LambdaWireBytes == 0);
    }

    private BitString Hash()
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);

        void Append(string name)
        {
            if (chunks.TryGetValue(name, out var payload)) hash.AppendData(payload.Span);
        }
        foreach (string name in BeamChecksumWire.BeforeLambda) Append(name);
        if (Optional(BeamfileMd5Names423.Lambdas, out var lambda))
        {
            var cursor = new BeamCursor(lambda);
            hash.AppendData(cursor.Read(BeamfileMd5Names423.Word).Span);
            Span<byte> zeros = stackalloc byte[BeamfileMd5Names423.HashOldUniqueBytes];
            zeros.Clear();
            while (cursor.Remaining != 0)
            {
                cancellation.ThrowIfCancellationRequested();
                hash.AppendData(cursor.Read(BeamfileMd5Names423.HashLambdaPrefixBytes).Span);
                cursor.Read(BeamfileMd5Names423.HashOldUniqueBytes);
                hash.AppendData(zeros);
            }
        }
        foreach (string name in BeamChecksumWire.AfterLambda) Append(name);

        return new BitString(hash.GetHashAndReset());
    }
}

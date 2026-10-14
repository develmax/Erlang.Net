using System.Buffers;
using System.Text;

namespace Erlang;

internal static class BeamStructureReader
{
    public static BeamStructure Read(ReadOnlyMemory<byte> input)
    {
        byte[] owned = input.ToArray();
        var outer = new BeamCursor(owned);
        if (!outer.Read(BeamIffWire.IdentifierBytes).Span.SequenceEqual(BeamIffWire.FormId))
        {
            throw new InvalidDataException(BeamDiagnostics.FormId);
        }
        var form = new BeamCursor(outer.Read(outer.ReadInt32()));
        if (!form.Read(BeamIffWire.IdentifierBytes).Span.SequenceEqual(BeamIffWire.FormType))
        {
            throw new InvalidDataException(BeamDiagnostics.FormType);
        }
        var chunks = new List<BeamChunk>();
        var selected = new Dictionary<string, BeamChunk>(StringComparer.Ordinal);
        while (form.Remaining != 0)
        {
            int offset = form.Position + BeamIffWire.FormHeaderBytes;
            string id = Encoding.ASCII.GetString(form.Read(BeamIffWire.IdentifierBytes).Span);
            int length = form.ReadInt32();
            if (length < 0 || length > BeamClrLimits.MaximumInputBytes - BeamIffWire.ChunkAlignmentMask)
            {
                throw new InvalidDataException(BeamDiagnostics.ChunkSize);
            }
            int padding = ((length + BeamIffWire.ChunkAlignmentMask) & ~BeamIffWire.ChunkAlignmentMask) - length;
            var chunk = new BeamChunk(
                id,
                offset,
                form.Read(length),
                padding
            );
            form.Read(padding);
            chunks.Add(chunk);
            selected[id] = chunk;
        }
        BeamCodeHeader code = ReadCode(Required(selected, BeamWire.CodeChunk));
        if (!selected.TryGetValue(BeamWire.AtomsChunk, out BeamChunk? atomsChunk) || atomsChunk.Payload.Length == 0)
        {
            throw new InvalidDataException(selected.TryGetValue(BeamWire.ObsoleteAtomsChunk, out BeamChunk? obsolete)
                && obsolete.Payload.Length != 0 ? BeamDiagnostics.ObsoleteAtoms : BeamDiagnostics.MissingAtoms);
        }
        IReadOnlyList<string?> atoms = ReadAtoms(atomsChunk.Payload);
        IReadOnlyList<BeamImport> imports = ReadImports(Required(selected, BeamWire.ImportsChunk), atoms.Count);
        IReadOnlyList<BeamExport> exports = ReadExports(Required(selected, BeamWire.ExportsChunk), atoms.Count);

        IReadOnlyList<Term> literals = selected.TryGetValue(BeamLiteralWire.Chunk, out BeamChunk? literalChunk) && literalChunk.Payload.Length != 0
            ? BeamLiteralReader.Read(literalChunk.Payload)
            : Array.Empty<Term>();

        return new(
            chunks.AsReadOnly(),
            atoms,
            imports,
            exports,
            code,
            literals,
            outer.Remaining
        );
    }

    private static ReadOnlyMemory<byte> Required(Dictionary<string, BeamChunk> chunks, string id)
    {
        if (!chunks.TryGetValue(id, out BeamChunk? chunk) || chunk.Payload.Length == 0)
        {
            throw new InvalidDataException(BeamDiagnostics.MissingChunk(id));
        }

        return chunk.Payload;
    }

    private static BeamCodeHeader ReadCode(ReadOnlyMemory<byte> payload)
    {
        var cursor = new BeamCursor(payload);
        int size = cursor.ReadInt32();
        var header = new BeamCursor(cursor.Read(size));
        int version = header.ReadInt32();
        if (version != BeamWire.FormatVersion)
        {
            throw new InvalidDataException(BeamDiagnostics.FormatVersion);
        }
        int opcode = header.ReadInt32();
        int labels = header.ReadInt32();
        int functions = header.ReadInt32();

        return new(
            version,
            opcode,
            labels,
            functions,
            cursor.Read(cursor.Remaining)
        );
    }

    private static IReadOnlyList<string?> ReadAtoms(ReadOnlyMemory<byte> payload)
    {
        var cursor = new BeamCursor(payload);
        int rawCount = cursor.ReadInt32();
        bool longLengths = rawCount < 0;
        if (rawCount == int.MinValue)
        {
            throw new InvalidDataException(BeamDiagnostics.AtomCount);
        }
        int count = Math.Abs(rawCount);
        CheckCount(count, 1, BeamWire.AtomEntryBytes);
        var atoms = new List<string?> { null };
        for (int index = 0; index < count; index++)
        {
            int length;
            if (longLengths)
            {
                BeamCompactValue value = BeamCompactReader.Read(cursor);
                if (!value.FitsNativeWord)
                {
                    throw new InvalidDataException(BeamDiagnostics.LongAtomLength);
                }
                ulong nativeLength = unchecked((ulong)(long)value.Value);
                if (nativeLength > BeamClrLimits.MaximumInputBytes)
                {
                    throw new InvalidDataException(BeamDiagnostics.AtomLengthBounds);
                }
                length = (int)nativeLength;
            }
            else
            {
                length = cursor.ReadByte();
            }
            ReadOnlySpan<byte> utf8 = cursor.Read(length).Span;
            var text = new StringBuilder();
            int characters = 0;
            while (!utf8.IsEmpty && characters < BeamWire.MaximumAtomCharacters)
            {
                if (Rune.DecodeFromUtf8(utf8, out Rune rune, out int consumed) != OperationStatus.Done)
                {
                    throw new InvalidDataException(BeamDiagnostics.Utf8Atom);
                }
                text.Append(rune.ToString());
                utf8 = utf8[consumed..];
                characters++;
            }
            atoms.Add(text.ToString());
        }

        return atoms.AsReadOnly();
    }

    private static IReadOnlyList<BeamImport> ReadImports(ReadOnlyMemory<byte> payload, int atoms)
    {
        var cursor = new BeamCursor(payload);
        int count = cursor.ReadInt32();
        CheckCount(count, 0, BeamWire.ImportEntryBytes);
        var entries = new List<BeamImport>();
        for (int index = 0; index < count; index++)
        {
            int module = cursor.ReadInt32();
            int function = cursor.ReadInt32();
            int arity = cursor.ReadInt32();
            CheckAtom(module, atoms);
            CheckAtom(function, atoms);
            CheckArity(arity);
            entries.Add(new(module, function, arity));
        }

        return entries.AsReadOnly();
    }

    private static IReadOnlyList<BeamExport> ReadExports(ReadOnlyMemory<byte> payload, int atoms)
    {
        var cursor = new BeamCursor(payload);
        int count = cursor.ReadInt32();
        CheckCount(count, 0, BeamWire.ExportEntryBytes);
        var entries = new List<BeamExport>();
        for (int index = 0; index < count; index++)
        {
            int function = cursor.ReadInt32();
            int arity = cursor.ReadInt32();
            int label = cursor.ReadInt32();
            CheckAtom(function, atoms);
            CheckArity(arity);
            if (label < 0)
            {
                throw new InvalidDataException(BeamDiagnostics.ExportLabel);
            }
            entries.Add(new(function, arity, label));
        }

        return entries.AsReadOnly();
    }

    private static void CheckCount(int count, int minimum, int entryBytes)
    {
        if (count < minimum || count >= BeamWire.TableAllocationSanityBytes / entryBytes)
        {
            throw new InvalidDataException(BeamDiagnostics.TableCount);
        }
    }

    private static void CheckAtom(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new InvalidDataException(BeamDiagnostics.AtomIndex);
        }
    }

    private static void CheckArity(int arity)
    {
        if (arity < 0 || arity > BeamWire.MaximumArity)
        {
            throw new InvalidDataException(BeamDiagnostics.FunctionArity);
        }
    }
}

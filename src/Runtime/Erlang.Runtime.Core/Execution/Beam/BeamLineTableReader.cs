using System.Buffers.Binary;
using System.Text;

namespace Erlang;

internal static class BeamLineTableReader
{
    internal static IReadOnlyList<Term> Read(BeamStructure structure)
    {
        BeamChunk? chunk = structure.Chunks.SingleOrDefault(value => value.Id == BeamLineNames.Chunk);
        if (chunk is null)
            return Array.Empty<Term>();
        var cursor = new BeamCursor(chunk.Payload);
        if (cursor.ReadInt32() != BeamLineNames.Version)
            return Array.Empty<Term>();
        _ = cursor.ReadInt32();
        int instructions = cursor.ReadInt32();
        int count = cursor.ReadInt32();
        int files = cursor.ReadInt32();
        if (instructions < 0 || count < 0 || count > cursor.Remaining || files < 0 || files > cursor.Remaining / BeamLineNames.FilenameLengthBytes)
            throw new InvalidDataException(BeamLineNames.Invalid);
        var positions = new (int File, int Line)[count];
        int file = 0;
        for (int index = 0; index < count; index++)
        {
            BeamCompactValue item = BeamCompactReader.Read(cursor);
            while (item.WireTag == BeamOperandParameters.AtomTag)
            {
                file = Index(item, files);
                item = BeamCompactReader.Read(cursor);
            }
            if (item.WireTag != BeamWire.IntegerTag)
                throw new InvalidDataException(BeamLineNames.Invalid);
            positions[index] = (file, Index(item, int.MaxValue));
        }
        var filenames = new string[files + 1];
        filenames[0] = structure.Atoms[1] + BeamLineNames.ErlangExtension;
        var encoding = new UTF8Encoding(false, true);
        for (int index = 1; index < filenames.Length; index++)
        {
            int length = BinaryPrimitives.ReadInt16BigEndian(cursor.Read(BeamLineNames.FilenameLengthBytes).Span);
            if (length < 0)
                throw new InvalidDataException(BeamLineNames.Invalid);
            filenames[index] = length == 0 ? filenames[0] : encoding.GetString(cursor.Read(length).Span);
        }
        if (cursor.Remaining != 0)
            throw new InvalidDataException(BeamLineNames.Invalid);
        var locations = new Term[count + 1];
        locations[0] = Nil.Value;
        for (int index = 0; index < positions.Length; index++)
        {
            var position = positions[index];
            locations[index + 1] = position.Line == 0 ? Nil.Value : Term.List(
                Term.Tuple(
                    Term.A(ErlangSourceLocationNames.File),
                    Cons.From(filenames[position.File].EnumerateRunes().Select(rune => Term.I(rune.Value)))
                ),
                Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(position.Line))
            );
        }

        return Array.AsReadOnly(locations);
    }

    private static int Index(BeamCompactValue value, int maximum)
    {
        if (!value.FitsNativeWord || value.Value < 0 || value.Value > maximum)
            throw new InvalidDataException(BeamLineNames.Invalid);

        return (int)value.Value;
    }
}

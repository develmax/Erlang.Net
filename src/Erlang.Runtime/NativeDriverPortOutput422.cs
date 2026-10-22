namespace Erlang;

internal static class NativeDriverPortOutput422
{
    private static Term Prefix(byte[] bytes, Term tail)
    {
        for (int index = bytes.Length - 1; index >= 0; index--)
            tail = new Cons(Term.I(bytes[index]), tail);

        return tail;
    }

    private static Term Body(NativeDriverPortRow422 row, byte[] bytes, bool present)
        => row.Options.Binary && present ? BitString.FromOwnedBytes(bytes)
            : Cons.From(bytes.Select(value => (Term)Term.I(value)));

    internal static IReadOnlyList<Term> Data(NativeDriverPortRow422 row, ErlDriverOutput420 output)
    {
        if (output.Header is null || output.Body is null || !Enum.IsDefined(output.Kind))
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        if (output.Kind == ErlDriverOutputKind420.Vector)
        {
            if (output.Segments is null || output.Segments.Length == 0 || output.Body.Length != 0)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            Term value = Nil.Value;
            for (int index = output.Segments.Length - 1; index >= 0; index--)
            {
                byte[] segment = output.Segments[index] ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
                if (!row.Options.Binary) value = Prefix(segment, value);
                else
                {
                    Term binary = BitString.FromOwnedBytes(segment);
                    // Native reverse fold uses NIL as the first-binary sentinel;
                    // even a final zero-length binary remains an improper tail.
                    value = value is Nil ? binary : new Cons(binary, value);
                }
            }

            return [Prefix(output.Header, value)];
        }
        if (output.Segments is not null) throw new InvalidDataException(DriverParentWireNames422.Invalid);
        if (output.Kind == ErlDriverOutputKind420.Bytes && row.Lines is not null)
        {
            var result = new List<Term>();
            foreach (PortLineResult line in row.Lines.Read(output.Body))
                result.Add(Term.Tuple(
                    Term.A(line.EndOfLine ? PortLineNames.EndOfLine : PortLineNames.NoEndOfLine),
                    Prefix(output.Header, Body(row, line.Data, true))
                ));

            return result;
        }
        Term body = Body(row, output.Body, output.Kind != ErlDriverOutputKind420.Bytes || output.BodyPresent);

        return [Prefix(output.Header, body)];
    }

    internal static IReadOnlyList<Term> Flush(NativeDriverPortRow422 row)
    {
        if (row.Lines is null) return [];

        return row.Lines.Flush().Select(line => (Term)Term.Tuple(
            Term.A(line.EndOfLine ? PortLineNames.EndOfLine : PortLineNames.NoEndOfLine),
            Body(row, line.Data, true))).ToArray();
    }
}

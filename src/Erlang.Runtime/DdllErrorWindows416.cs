using System.Runtime.InteropServices;

namespace Erlang;

internal static class DdllErrorWindows416
{
    [DllImport(DdllErrorNames416.Library, EntryPoint = DdllErrorNames416.FormatMessage, ExactSpelling = true)]
    private static extern uint FormatMessage(
        uint flags,
        nint source,
        uint message,
        uint language,
        [Out] byte[] buffer,
        uint size,
        nint arguments
    );

    internal static Term Format(Term input)
    {
        if (input is Atom atom)
        {
            string? description = DdllErrorDescriptions416.Atom(atom.Name);
            if (description is null) throw new ErlangException(ErlangErrorReasons.BadArgument);

            return Ascii(description);
        }
        if (input is not TupleTerm { Items.Count: 2 } tuple
            || tuple.Items[0] is not Atom { Name: DdllErrorNames416.OpenError }
            || tuple.Items[1] is not Integer integer
            || integer.Value < DdllErrorParameters416.SmallMinimum
            || integer.Value > DdllErrorParameters416.SmallMaximum)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        // Native errdesc_to_code narrows signed_val(Small) to its C int out parameter.
        int code = unchecked((int)(long)integer.Value);
        string? text = DdllErrorDescriptions416.Code(code);
        if (text is not null) return Ascii(text);
        if (code > DdllErrorParameters416.DynamicErrorOffset) return Ascii(DdllErrorNames416.Unspecified);

        uint message = unchecked((uint)-(code - DdllErrorParameters416.DynamicErrorOffset));
        byte[] bytes = new byte[DdllErrorParameters416.BufferBytes];
        uint length = FormatMessage(
            DdllErrorParameters416.FromSystem,
            0,
            message,
            DdllErrorParameters416.NeutralDefaultLanguage,
            bytes,
            (uint)bytes.Length,
            0
        );
        if (length == 0) return Ascii(DdllErrorNames416.Unspecified);
        int count = 0;
        while (count < bytes.Length && bytes[count] != 0) count++;
        while (count > 0 && bytes[count - 1] is DdllErrorParameters416.CarriageReturn or DdllErrorParameters416.LineFeed) count--;

        return Bytes(bytes.AsSpan(0, count));
    }

    private static Term Ascii(string value)
    {
        Term result = Nil.Value;
        for (int index = value.Length - 1; index >= 0; index--)
            result = new Cons(Term.I(value[index]), result);

        return result;
    }

    private static Term Bytes(ReadOnlySpan<byte> value)
    {
        Term result = Nil.Value;
        for (int index = value.Length - 1; index >= 0; index--)
            result = new Cons(Term.I(value[index]), result);

        return result;
    }
}

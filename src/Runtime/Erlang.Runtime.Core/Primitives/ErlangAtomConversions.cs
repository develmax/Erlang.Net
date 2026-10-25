using System.Text;
using System.Buffers;

namespace Erlang;

internal static class ErlangAtomConversions
{
    public static string EncodingName(IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 1)
            return ErlangAtomParameters.Utf8;
        if (arguments[1] is not Atom { Name: ErlangAtomParameters.Utf8 or ErlangAtomParameters.Unicode or ErlangAtomParameters.Latin1 } encoding)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return encoding.Name;
    }

    public static Term FromList(Term value, bool existing)
    {
        var text = new StringBuilder();
        int count = 0;
        foreach (Term item in Cons.Items(value))
        {
            if (count++ == ErlangAtomParameters.MaximumCodePoints)
                throw new ErlangException(existing ? ErlangErrorReasons.BadArgument : ErlangErrorReasons.SystemLimit);
            if (item is not Integer number || number.Value < 0 || number.Value > int.MaxValue || !Rune.IsValid((int)number.Value))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            text.Append(new Rune((int)number.Value));
        }

        return Create(text.ToString(), existing);
    }

    public static Term FromBinary(IReadOnlyList<Term> arguments, bool existing)
    {
        byte[] bytes = ErlangIoData.Binary(arguments[0]);
        string encoding = EncodingName(arguments);
        int limit = encoding == ErlangAtomParameters.Latin1 ? ErlangAtomParameters.MaximumCodePoints : ErlangAtomParameters.MaximumUtf8Bytes;
        if (bytes.Length > limit)
            throw new ErlangException(existing ? ErlangErrorReasons.BadArgument : ErlangErrorReasons.SystemLimit);
        try
        {
            string name;
            if (encoding == ErlangAtomParameters.Latin1)
                name = Encoding.Latin1.GetString(bytes);
            else
            {
                var text = new StringBuilder();
                int offset = 0;
                int count = 0;
                while (offset < bytes.Length)
                {
                    if (count++ == ErlangAtomParameters.MaximumCodePoints)
                        throw new ErlangException(existing ? ErlangErrorReasons.BadArgument : ErlangErrorReasons.SystemLimit);
                    if (Rune.DecodeFromUtf8(bytes.AsSpan(offset), out Rune rune, out int consumed) != OperationStatus.Done)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    text.Append(rune);
                    offset += consumed;
                }
                name = text.ToString();
            }

            return Create(name, existing);
        }
        catch (DecoderFallbackException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    public static Term ToBinary(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom atom)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        string encoding = EncodingName(arguments);
        if (encoding == ErlangAtomParameters.Latin1)
        {
            if (atom.Name.EnumerateRunes().Any(rune => rune.Value > ErlangAtomParameters.MaximumLatin1CodePoint))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return atom.Name.All(char.IsAscii)
                ? AtomUtf8BinaryCache.Get(atom)
                : new BitString(Encoding.Latin1.GetBytes(atom.Name));
        }

        try
        {
            return AtomUtf8BinaryCache.Get(atom);
        }
        catch (EncoderFallbackException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static Atom Create(string name, bool existing)
    {
        if (existing && !AtomNameRegistry.Exists(name))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.A(name);
    }
}

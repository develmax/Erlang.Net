using System.Globalization;

namespace Erlang;

internal static class ErlangFloatText
{
    public static FloatTerm Parse(IEnumerable<int> characters)
    {
        string text = new string(characters.Select(character => (char)character).ToArray());
        int terminator = text.IndexOf('\0');
        if (terminator >= 0)
            text = text[..terminator];
        int position = 0;
        if (position < text.Length && text[position] is '+' or '-')
            position++;
        if (!Digits())
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (position >= text.Length || text[position] is not ('.' or ','))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        position++;
        if (!Digits())
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (position < text.Length && text[position] is 'e' or 'E')
        {
            position++;
            if (position < text.Length && text[position] is '+' or '-')
                position++;
            if (!Digits())
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (position != text.Length || !double.TryParse(
            text.Replace(',', '.'),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out double value
        ) || !double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new FloatTerm(value);

        bool Digits()
        {
            int start = position;
            while (position < text.Length && text[position] is >= '0' and <= '9')
                position++;

            return position > start;
        }
    }
}

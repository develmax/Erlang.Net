using System.Text;

namespace Erlang.Compiler;

internal static class CharacterLiteralLexer
{
    public static int Read(string text, ref int position, int start)
    {
        int value = ReadRune(text, ref position, start);
        if (value != CharacterLiteralParameters.Escape)
            return value;
        int escape = ReadRune(text, ref position, start);
        if (escape is >= CharacterLiteralParameters.FirstOctalDigit and <= CharacterLiteralParameters.LastOctalDigit)
        {
            value = escape - CharacterLiteralParameters.FirstOctalDigit;
            int digits = 1;
            while (digits < CharacterLiteralParameters.MaximumOctalDigits && position < text.Length && text[position] is >= CharacterLiteralParameters.FirstOctalDigit and <= CharacterLiteralParameters.LastOctalDigit)
            {
                value = value * CharacterLiteralParameters.OctalRadix + text[position++] - CharacterLiteralParameters.FirstOctalDigit;
                digits++;
            }

            return value;
        }
        if (escape == CharacterLiteralParameters.HexMarker)
            return ReadHex(text, ref position, start);
        if (escape == CharacterLiteralParameters.ControlMarker)
        {
            value = ReadRune(text, ref position, start);
            if (value == CharacterEscapeSyntax.ControlDelete)
                return CharacterLiteralParameters.DeleteCodepoint;
            if (value is >= CharacterEscapeSyntax.FirstControlUppercase and <= CharacterEscapeSyntax.LastControlUppercase or >= CharacterEscapeSyntax.FirstControlLowercase and <= CharacterEscapeSyntax.LastControlLowercase)
                return value & CharacterLiteralParameters.ControlMask;

            throw Illegal(start);
        }

        return escape switch
        {
            CharacterEscapeSyntax.Newline => '\n',
            CharacterEscapeSyntax.CarriageReturn => '\r',
            CharacterEscapeSyntax.Tab => '\t',
            CharacterEscapeSyntax.VerticalTab => '\v',
            CharacterEscapeSyntax.Backspace => '\b',
            CharacterEscapeSyntax.FormFeed => '\f',
            CharacterEscapeSyntax.Escape => CharacterLiteralParameters.EscapeCodepoint,
            CharacterEscapeSyntax.Space => ' ',
            CharacterEscapeSyntax.Delete => CharacterLiteralParameters.DeleteCodepoint,
            _ => escape
        };
    }

    private static int ReadRune(string text, ref int position, int start)
    {
        if (position == text.Length)
            throw Unterminated(start);
        if (!Rune.TryGetRuneAt(text, position, out var rune) || !IsLegal(rune.Value))
            throw Illegal(start);
        position += rune.Utf16SequenceLength;

        return rune.Value;
    }

    private static int ReadHex(string text, ref int position, int start)
    {
        if (position == text.Length)
            throw Unterminated(start);
        bool braced = text[position] == CharacterLiteralParameters.HexOpening;
        if (braced)
            position++;
        int value = 0, digits = 0;
        bool overflow = false;
        while (position < text.Length && HexValue(text[position]) is int digit && digit >= 0 && (braced || digits < CharacterLiteralParameters.FixedHexDigits))
        {
            if (value > (CharacterLiteralParameters.MaximumUnicodeScalar - digit) / CharacterLiteralParameters.HexRadix)
                overflow = true;
            if (!overflow)
                value = value * CharacterLiteralParameters.HexRadix + digit;
            position++;
            digits++;
        }
        if (braced)
        {
            if (position == text.Length)
                throw Unterminated(start);
            if (text[position++] != CharacterLiteralParameters.HexClosing || digits == 0)
                throw Illegal(start);
        }
        else if (digits != CharacterLiteralParameters.FixedHexDigits)
            throw Illegal(start);
        if (overflow || !IsLegal(value))
            throw Illegal(start);

        return value;
    }

    private static int HexValue(char value) => value switch
    {
        >= CharacterEscapeSyntax.FirstDecimalDigit and <= CharacterEscapeSyntax.LastDecimalDigit => value - CharacterEscapeSyntax.FirstDecimalDigit,
        >= CharacterEscapeSyntax.FirstUpperHex and <= CharacterEscapeSyntax.LastUpperHex => value - CharacterEscapeSyntax.FirstUpperHex + CharacterLiteralParameters.HexAlphabetOffset,
        >= CharacterEscapeSyntax.FirstLowerHex and <= CharacterEscapeSyntax.LastLowerHex => value - CharacterEscapeSyntax.FirstLowerHex + CharacterLiteralParameters.HexAlphabetOffset,
        _ => -1
    };

    private static bool IsLegal(int value) => value >= 0 && value <= CharacterLiteralParameters.MaximumUnicodeScalar && value is not (>= CharacterLiteralParameters.FirstSurrogate and <= CharacterLiteralParameters.LastSurrogate) && value is not CharacterLiteralParameters.IllegalBmpCodepoint and not CharacterLiteralParameters.IllegalBmpCodepointNext;

    private static CompileException Illegal(int start) => new(CompilerDiagnosticCodes.InvalidLiteral, CharacterLiteralDiagnostics.Illegal, start);

    private static CompileException Unterminated(int start) => new(CompilerDiagnosticCodes.InvalidLiteral, CharacterLiteralDiagnostics.Unterminated, start);
}

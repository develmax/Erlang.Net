using System.Globalization;
using System.Numerics;

namespace Erlang.Compiler;

internal static class BasedIntegerLexer
{
    public static Token Read(string text, int start, ref int index)
    {
        BigInteger radixValue = 0;
        for (int position = start; position < index; position++)
        {
            char digit = text[position];
            if (digit == BasedIntegerParameters.Separator && position > start && position + 1 < index && Decimal(text[position - 1]) && Decimal(text[position + 1]))
                continue;
            if (!Decimal(digit))
                throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, position);
            radixValue = radixValue * BasedIntegerParameters.DecimalBase + digit - '0';
        }
        if (radixValue < BasedIntegerParameters.MinimumBase || radixValue > BasedIntegerParameters.MaximumBase)
            throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.InvalidBase(radixValue), index);
        int radix = (int)radixValue;
        index++;
        int firstDigit = index;
        BigInteger value = 0;
        while (index < text.Length)
        {
            int digit = Digit(text[index]);
            if (digit >= 0 && digit < radix)
            {
                value = value * radix + digit;
                index++;
                continue;
            }
            if (text[index] == BasedIntegerParameters.Separator)
            {
                int next = index + 1 < text.Length ? Digit(text[index + 1]) : -1;
                if (index == firstDigit || next < 0 || next >= radix)
                    throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, index);
                index++;
                continue;
            }
            if (text[index] == '.' && index + 1 < text.Length)
            {
                int next = Digit(text[index + 1]);
                if (index > firstDigit && next >= 0 && next < radix)
                    return ReadFloat(text, start, firstDigit, radix, ref index);
                if (NameCharacter(text[index + 1]))
                    throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalFloat, index);
            }
            if (NameCharacter(text[index]))
                throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, index);
            break;
        }
        if (index == firstDigit)
            throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, index);

        return new Token(LexerTokenKinds.Integer, value.ToString(CultureInfo.InvariantCulture), start, index);
    }

    private static Token ReadFloat(
        string text,
        int start,
        int firstDigit,
        int radix,
        ref int index
    )
    {
        int point = index++;
        int fractionStart = index;
        ReadDigits(text, radix, ref index);
        string fraction = text[fractionStart..index].Replace(NumericLiteralSyntax.DigitSeparator, string.Empty);
        string integer = text[firstDigit..point].Replace(NumericLiteralSyntax.DigitSeparator, string.Empty);

        BigInteger exponent = BigInteger.Zero;
        if (index + 1 < text.Length && text[index] == BasedIntegerParameters.BaseMarker && text[index + 1] is 'e' or 'E')
        {
            index += 2;
            int exponentStart = index;
            if (index < text.Length && text[index] is '+' or '-')
            {
                index++;
            }

            ReadDigits(text, BasedIntegerParameters.DecimalBase, ref index);
            string exponentText = text[exponentStart..index].Replace(NumericLiteralSyntax.DigitSeparator, string.Empty);
            if (!BigInteger.TryParse(exponentText, CultureInfo.InvariantCulture, out exponent))
            {
                throw InvalidFloat(start);
            }
        }
        else if (index < text.Length && NameCharacter(text[index]))
        {
            throw InvalidFloat(index);
        }

        double value;
        if (radix == BasedIntegerParameters.DecimalBase)
        {
            string decimalText = $"{integer}.{fraction}e{exponent.ToString(CultureInfo.InvariantCulture)}";
            if (!double.TryParse(decimalText, CultureInfo.InvariantCulture, out value))
            {
                throw InvalidFloat(start);
            }
        }
        else
        {
            int fractionDigits = Math.Max(1, fraction.TrimEnd('0').Length);
            BigInteger significand = 0;
            foreach (char digit in integer.Concat(fraction.Take(fractionDigits)))
            {
                significand = significand * radix + Digit(digit);
            }

            double coefficient = double.Parse(significand.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            value = coefficient * Math.Pow(radix, (double)(exponent - fractionDigits));
        }

        if (!double.IsFinite(value))
        {
            throw InvalidFloat(start);
        }

        return new Token(LexerTokenKinds.Float, value.ToString("R", CultureInfo.InvariantCulture), start, index);
    }

    private static void ReadDigits(string text, int radix, ref int index)
    {
        int start = index;
        while (index < text.Length)
        {
            int digit = Digit(text[index]);
            if (digit >= 0 && digit < radix)
            {
                index++;
                continue;
            }

            if (text[index] != BasedIntegerParameters.Separator)
            {
                break;
            }

            int next = index + 1 < text.Length ? Digit(text[index + 1]) : -1;
            if (index == start || next < 0 || next >= radix)
            {
                throw InvalidFloat(index);
            }

            index++;
        }
    }

    private static CompileException InvalidFloat(int offset) =>
        new(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalFloat, offset);

    private static bool Decimal(char value) => value is >= '0' and <= '9';

    private static int Digit(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'A' and <= 'Z' => value - 'A' + BasedIntegerParameters.DecimalBase,
        >= 'a' and <= 'z' => value - 'a' + BasedIntegerParameters.DecimalBase,
        _ => -1
    };

    private static bool NameCharacter(char value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '@';
}

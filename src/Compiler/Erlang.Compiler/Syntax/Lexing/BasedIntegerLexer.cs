using System.Globalization;
using System.Numerics;

namespace Erlang.Compiler;

internal static class BasedIntegerLexer
{
    public static string Read(string text, int start, ref int index)
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
                if (next >= 0 && next < radix)
                    throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, BasedIntegerDiagnostics.BasedFloatUnsupported, start);
                if (NameCharacter(text[index + 1]))
                    throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalFloat, index);
            }
            if (NameCharacter(text[index]))
                throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, index);
            break;
        }
        if (index == firstDigit)
            throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, BasedIntegerDiagnostics.IllegalInteger, index);

        return value.ToString(CultureInfo.InvariantCulture);
    }

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

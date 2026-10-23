using System.Globalization;
using System.Text;

namespace Erlang.Otp;

internal static class ProcLibReportAtomWriter
{
    public static Term Write(Term value, Term encoding)
    {
        if (value is not Atom atom)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var scalars = atom.Name.EnumerateRunes().ToArray();
        bool quote = scalars.Length == 0 || ProcLibReportAtomNames.Reserved.Contains(atom.Name) ||
            !IsLower(scalars[0].Value) || scalars.Skip(1).Any(rune => !IsName(rune.Value));
        if (!quote)
            return Term.String(atom.Name);
        bool latin1 = encoding is Atom { Name: ProcLibReportAtomNames.Latin1 };
        var output = new StringBuilder();
        output.Append('\'');
        foreach (Rune rune in scalars)
        {
            int character = rune.Value;
            if (character is '\'' or '\\')
                output.Append('\\').Append(rune.ToString());
            else if (character is >= ProcLibReportCharacterParameters.PrintableAsciiFirst and <= ProcLibReportCharacterParameters.PrintableAsciiLast ||
                character >= ProcLibReportCharacterParameters.PrintableLatin1First && (!latin1 || character <= ProcLibReportCharacterParameters.Latin1Maximum))
                output.Append(rune.ToString());
            else if (character > ProcLibReportCharacterParameters.Latin1Maximum)
                output.Append(ProcLibReportAtomNames.HexPrefix).Append(character.ToString("X", CultureInfo.InvariantCulture)).Append(ProcLibReportAtomNames.HexSuffix);
            else
            {
                char escape = character switch
                {
                    '\n' => 'n',
                    '\r' => 'r',
                    '\t' => 't',
                    '\v' => 'v',
                    '\b' => 'b',
                    '\f' => 'f',
                    '\u001b' => 'e',
                    '\u007f' => 'd',
                    _ => '\0'
                };
                output.Append('\\');
                if (escape != '\0')
                    output.Append(escape);
                else
                    output.Append(Convert.ToString(character, 8).PadLeft(3, '0'));
            }
        }
        output.Append('\'');

        return Term.String(output.ToString());
    }

    private static bool IsLower(int character)
        => character is >= 'a' and <= 'z' ||
            character is >= ProcLibReportCharacterParameters.LowerLatin1First and <= ProcLibReportCharacterParameters.LowerLatin1Last &&
                character != ProcLibReportCharacterParameters.DivisionSign;

    private static bool IsName(int character)
        => IsLower(character) || character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '@' ||
            character is >= ProcLibReportCharacterParameters.UpperLatin1First and <= ProcLibReportCharacterParameters.UpperLatin1Last &&
                character != ProcLibReportCharacterParameters.MultiplicationSign;
}

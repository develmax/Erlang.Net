using System.Globalization;
using System.Text;

namespace Erlang;

internal static class FunctionText
{
    private const string LocalPrefix = "#Fun<";
    private const string LocalSuffix = ">";
    private const string LocalSeparator = ".";
    private const string ExternalPrefix = "fun ";
    private const string ExternalModuleSeparator = ":";
    private const string ExternalAritySeparator = "/";
    private const int FirstLowerLatin1 = 223;
    private const int LastLatin1 = 255;
    private const int Latin1Division = 247;
    private const int FirstUpperLatin1 = 192;
    private const int LastUpperLatin1 = 222;
    private const int Latin1Multiplication = 215;
    private const int FirstControlLimit = 32;
    private const int FirstExtendedControl = 128;
    private const int LastExtendedControl = 159;
    private const int OctalRadix = 8;
    private const int OctalWidth = 3;
    private const string QuoteEscape = "\\'";
    private const string SlashEscape = "\\\\";
    private const string NewlineEscape = "\\n";
    private const string FormFeedEscape = "\\f";
    private const string TabEscape = "\\t";
    private const string CarriageReturnEscape = "\\r";
    private const string BackspaceEscape = "\\b";
    private const string VerticalTabEscape = "\\v";

    public static string Format(FunctionTerm function)
    {
        if (function.LocalIdentity is { } local)
            return LocalPrefix + local.Module.Name + LocalSeparator + local.Index.ToString(CultureInfo.InvariantCulture) + LocalSeparator + local.OldUnique.ToString(CultureInfo.InvariantCulture) + LocalSuffix;
        var external = function.ExternalIdentity!;

        return ExternalPrefix + AtomName(external.Module.Name) + ExternalModuleSeparator + AtomName(external.Function.Name) + ExternalAritySeparator + function.Arity.ToString(CultureInfo.InvariantCulture);
    }

    private static bool Lower(int value) => value is >= 'a' and <= 'z' || value >= FirstLowerLatin1 && value <= LastLatin1 && value != Latin1Division;

    private static bool Alphanumeric(int value) => Lower(value) || value is >= 'A' and <= 'Z' or >= '0' and <= '9' || value >= FirstUpperLatin1 && value <= LastUpperLatin1 && value != Latin1Multiplication;

    private static string AtomName(string name)
    {
        var runes = name.EnumerateRunes().ToArray();
        bool quote = runes.Length == 0 || !Lower(runes[0].Value) || runes.Skip(1).Any(rune => !Alphanumeric(rune.Value) && rune.Value != '_');
        var result = new StringBuilder();
        if (quote)
            result.Append('\'');
        foreach (var rune in runes)
        {
            switch (rune.Value)
            {
                case '\'':
                    result.Append(QuoteEscape);
                    break;
                case '\\':
                    result.Append(SlashEscape);
                    break;
                case '\n':
                    result.Append(NewlineEscape);
                    break;
                case '\f':
                    result.Append(FormFeedEscape);
                    break;
                case '\t':
                    result.Append(TabEscape);
                    break;
                case '\r':
                    result.Append(CarriageReturnEscape);
                    break;
                case '\b':
                    result.Append(BackspaceEscape);
                    break;
                case '\v':
                    result.Append(VerticalTabEscape);
                    break;
                default:
                    if (rune.Value < FirstControlLimit || rune.Value >= FirstExtendedControl && rune.Value <= LastExtendedControl)
                        result.Append('\\').Append(Convert.ToString(rune.Value, OctalRadix).PadLeft(OctalWidth, '0'));
                    else
                        result.Append(rune);
                    break;
            }
        }
        if (quote)
            result.Append('\'');

        return result.ToString();
    }
}

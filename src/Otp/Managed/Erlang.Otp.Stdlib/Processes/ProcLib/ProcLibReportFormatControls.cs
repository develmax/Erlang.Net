using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal sealed class ProcLibReportFormatControls(IProcLibReportTermPrinter printer)
{
    public Term Format(string format, IReadOnlyList<Term> arguments, Term charsLimit)
    {
        var controls = Collect(format, arguments);
        BigInteger limit = charsLimit is Integer integer ? integer.Value : ProcLibReportControlNames.Unlimited;
        int count = controls.Count(item => item is ProcLibReportControl);
        int prettyCount = controls.Count(item => item is ProcLibReportControl { Directive: ProcLibReportControlNames.Pretty or ProcLibReportControlNames.DepthPretty });
        int other = controls.Count - count;
        limit = Subtract(limit, other);
        var output = new List<Term>();
        int indent = 0;
        foreach (object item in controls)
        {
            if (item is int character)
            {
                output.Add(Term.I(character));
                indent = AdvanceIndent(character, indent);
                continue;
            }
            var control = (ProcLibReportControl)item;
            BigInteger maximum = limit.Sign < 0 ? limit : limit / count;
            Term value;
            if (control.Directive == ProcLibReportControlNames.Text)
            {
                if (control.Unicode && maximum.Sign >= 0)
                    throw new NotSupportedException(ProcLibReportControlNames.LimitedUnicodeTextOutsideReportSubset);
                value = control.Unicode
                    ? ProcLibReportControlText.Unicode(control.Argument)
                    : ProcLibReportControlText.Latin1(control.Argument, maximum);
            }
            else if (control.Directive == ProcLibReportControlNames.Write)
            {
                if (maximum.Sign >= 0)
                    throw new NotSupportedException(ProcLibReportControlNames.LimitedWriteOutsideReportSubset);
                value = printer.Write(control.Argument, control.Unicode ? Term.A(ProcLibReportControlNames.Unicode) : ProcLibReportFormatAtoms.Latin1);
            }
            else
            {
                if (control.Depth is not Integer)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                value = printer.Render(
                    control.Argument,
                    control.Depth,
                    control.Unicode ? Term.A(ProcLibReportControlNames.Unicode) : ProcLibReportFormatAtoms.Latin1,
                    new Integer(maximum),
                    control.Width ?? ProcLibReportControlNames.DefaultWidth,
                    control.Column ?? indent + 1
                );
                prettyCount--;
            }
            output.Add(value);
            count--;
            if (limit.Sign >= 0)
                limit = Subtract(limit, ProcLibReportControlText.Characters(value).Count());
            if (prettyCount > 0)
                foreach (int codepoint in ProcLibReportControlText.Characters(value))
                    indent = AdvanceIndent(codepoint, indent);
        }

        return Cons.From(output);
    }

    private static List<object> Collect(string format, IReadOnlyList<Term> arguments)
    {
        var output = new List<object>();
        int argument = 0;
        for (int index = 0; index < format.Length; index++)
        {
            char character = format[index];
            if (character != ProcLibReportControlNames.Prefix)
            {
                output.Add((int)character);
                continue;
            }
            index++;
            int? width = Number(format, ref index);
            int? column = null;
            if (index < format.Length && format[index] == ProcLibReportControlNames.Precision)
            {
                index++;
                column = Number(format, ref index) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            bool unicode = index < format.Length && format[index] == ProcLibReportControlNames.UnicodeModifier;
            if (unicode)
                index++;
            if (index >= format.Length || argument >= arguments.Count)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            char directive = format[index];
            if (directive is not ProcLibReportControlNames.Text and not ProcLibReportControlNames.Pretty and not ProcLibReportControlNames.DepthPretty and not ProcLibReportControlNames.Write)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Term value = arguments[argument++];
            Term depth = Term.I(ProcLibReportControlNames.Unlimited);
            if (directive == ProcLibReportControlNames.DepthPretty)
            {
                if (argument >= arguments.Count)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                depth = arguments[argument++];
            }
            if (directive == ProcLibReportControlNames.Text && (width is not null || column is not null))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (directive == ProcLibReportControlNames.Text && value is Atom atom)
            {
                foreach (var rune in atom.Name.EnumerateRunes())
                {
                    if (!unicode && rune.Value > ProcLibReportControlNames.Latin1Maximum)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    output.Add(rune.Value);
                }
            }
            else
            {
                output.Add(new ProcLibReportControl(
                    directive,
                    value,
                    depth,
                    unicode,
                    width,
                    column
                ));
            }
        }
        if (argument != arguments.Count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return output;
    }

    private static int? Number(string format, ref int index)
    {
        int? result = null;
        while (index < format.Length && format[index] is >= ProcLibReportControlNames.FirstDigit and <= ProcLibReportControlNames.LastDigit)
        {
            try
            {
                result = checked((result ?? 0) * ProcLibReportControlNames.DigitRadix + format[index] - ProcLibReportControlNames.FirstDigit);
            }
            catch (OverflowException)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            index++;
        }

        return result;
    }

    private static BigInteger Subtract(BigInteger limit, BigInteger count)
        => limit.Sign < 0 ? limit : BigInteger.Max(limit - count, BigInteger.Zero);

    private static int AdvanceIndent(int character, int indent)
        => character == ProcLibReportControlNames.Newline ? 0
            : character == ProcLibReportControlNames.Tab
                ? (indent + ProcLibReportControlNames.TabWidth) / ProcLibReportControlNames.TabWidth * ProcLibReportControlNames.TabWidth
                : indent + 1;
}

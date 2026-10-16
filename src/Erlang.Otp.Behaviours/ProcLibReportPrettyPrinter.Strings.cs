using System.Buffers;
using System.Globalization;
using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportPrettyPrinter
{
    private bool TryString(
        Cons list,
        int budget,
        bool latin1,
        out string? text,
        out bool truncated
    )
    {
        if (!latin1 && budget >= 0)
            return TryUnicodePrefix(
                list,
                Math.Max(0, budget - 2),
                out text,
                out truncated
            );

        var output = new StringBuilder();
        Term remaining = list;
        int maximum = budget < 0 ? int.MaxValue : Math.Max(0, budget - 2);
        while (remaining is Cons cell)
        {
            if (output.Length == maximum)
            {
                text = output.ToString();
                truncated = true;

                return true;
            }
            if (cell.Head is not Integer integer || integer.Value < 0 || integer.Value > ProcLibReportCharacterParameters.UnicodeMaximum)
                break;

            int character = (int)integer.Value;
            if (!Printable(character, latin1 || !unicodePrintable))
                break;

            output.Append(char.ConvertFromUtf32(character));
            remaining = cell.Tail;
        }
        text = output.ToString();
        truncated = false;

        return remaining is Nil;
    }

    private bool TryUnicodePrefix(
        Cons list,
        int maximum,
        out string? text,
        out bool truncated
    )
    {
        text = null;
        truncated = false;
        if (maximum == 0)
            return false;

        var output = new StringBuilder();
        try
        {
            foreach (int character in UnicodeCharacters(list))
            {
                output.Append(char.ConvertFromUtf32(character));
                string available = output.ToString();
                int[] boundaries = StringInfo.ParseCombiningCharacters(available);
                if (boundaries.Length <= maximum)
                    continue;

                string prefix = available[..boundaries[maximum]];
                if (!FlatPrefix(list, prefix.EnumerateRunes().Count()) ||
                    prefix.EnumerateRunes().Any(rune => !Printable(rune.Value, !unicodePrintable)))
                    return false;

                text = prefix;
                truncated = true;

                return true;
            }
        }
        catch (ErlangException)
        {
            return false;
        }
        string all = output.ToString();
        if (all.Length == 0 || !FlatPrefix(list, all.EnumerateRunes().Count(), complete: true) ||
            all.EnumerateRunes().Any(rune => !Printable(rune.Value, !unicodePrintable)))
            return false;

        text = all;

        return true;
    }

    private static bool FlatPrefix(Term list, int characters, bool complete = false)
    {
        while (characters-- > 0)
        {
            if (list is not Cons { Head: Integer } cell)
                return false;

            list = cell.Tail;
        }

        return !complete || list is Nil;
    }

    private static IEnumerable<int> UnicodeCharacters(Term characters)
    {
        var pending = new Stack<Term>();
        pending.Push(characters);
        while (pending.TryPop(out var value))
        {
            switch (value)
            {
                case Nil:
                    break;
                case Cons cell:
                    pending.Push(cell.Tail);
                    pending.Push(cell.Head);
                    break;
                case Integer integer when integer.Value >= 0 && integer.Value <= ProcLibReportCharacterParameters.UnicodeMaximum && Rune.IsValid((int)integer.Value):
                    yield return (int)integer.Value;
                    break;
                case BitString { IsBinary: true } binary:
                    {
                        byte[] bytes = binary.ToArray();
                        int offset = 0;
                        while (offset < bytes.Length)
                        {
                            if (Rune.DecodeFromUtf8(bytes.AsSpan(offset), out Rune rune, out int consumed) != OperationStatus.Done)
                                throw new ErlangException(ErlangErrorReasons.BadArgument);

                            offset += consumed;
                            yield return rune.Value;
                        }
                        break;
                    }
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    private static bool Printable(int character, bool latin1)
        => character is >= ProcLibReportCharacterParameters.PrintableAsciiFirst and <= ProcLibReportCharacterParameters.PrintableAsciiLast or >= ProcLibReportCharacterParameters.PrintableLatin1First and <= ProcLibReportCharacterParameters.Latin1Maximum or '\t' or '\n' or '\r' or '\v' or '\b' or '\f' or 0x1b ||
            !latin1 && character is >= ProcLibReportPrettyParameters.UnicodeBmpFirst and <= ProcLibReportPrettyParameters.UnicodeBeforeSurrogatesLast or >= ProcLibReportPrettyParameters.UnicodeAfterSurrogatesFirst and <= ProcLibReportPrettyParameters.UnicodeBmpLast or >= ProcLibReportPrettyParameters.UnicodeSupplementaryFirst and <= ProcLibReportCharacterParameters.UnicodeMaximum;

    private static string Quote(string text)
    {
        var output = new StringBuilder();
        output.Append('"');
        foreach (Rune rune in text.EnumerateRunes())
        {
            char escape = rune.Value switch
            {
                '"' => '"',
                '\\' => '\\',
                '\n' => 'n',
                '\r' => 'r',
                '\t' => 't',
                '\v' => 'v',
                '\b' => 'b',
                '\f' => 'f',
                0x1b => 'e',
                _ => '\0'
            };
            if (escape != '\0')
                output.Append('\\').Append(escape);
            else
                output.Append(rune.ToString());
        }
        output.Append('"');

        return output.ToString();
    }

    private ProcLibReportPrettyNode BuildBinary(
        BitString binary,
        int depth,
        int budget,
        bool latin1
    )
    {
        if (binary.BitLength == 0)
            return ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.EmptyBinary);
        if (depth == 1)
            return WithMore(
                ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.BinaryOpen + ProcLibReportPrettyNames.Ellipsis + ProcLibReportPrettyNames.BinaryClose, 3),
                (nextBudget, addedDepth) => BuildBinary(
                    binary,
                    1 + addedDepth,
                    nextBudget,
                    latin1
                )
            );

        byte[] bytes = binary.ToArray();
        int characterDepth = Decrement(depth);
        int printableBudget = Subtract(budget, ProcLibReportPrettyParameters.BinaryStringDelimiters);
        long depthLimit = characterDepth < 0 ? bytes.Length : Math.Min((long)ProcLibReportPrettyParameters.PrintableBinaryCharactersPerDepth * characterDepth, bytes.Length);
        int maximum = (int)(printableBudget < 0 ? depthLimit : Math.Min(depthLimit, printableBudget));
        if (printStrings && binary.IsBinary && maximum > 0 &&
            TryBinaryString(
                bytes,
                maximum,
                characterDepth,
                latin1,
                out string? text,
                out bool utf8,
                out bool truncated
            ))
        {
            string rendered = ProcLibReportPrettyNames.BinaryOpen + Quote(text!) +
                (utf8 ? ProcLibReportPrettyNames.Utf8Suffix : string.Empty) +
                (truncated ? ProcLibReportPrettyNames.Ellipsis : string.Empty) + ProcLibReportPrettyNames.BinaryClose;

            Term quoted = Term.String(Quote(text!));
            Term raw;
            if (truncated)
                raw = Cons.From(
                    [Term.I('<'), Term.I('<'), quoted],
                    Term.String((utf8 ? ProcLibReportPrettyNames.Utf8Suffix : string.Empty) + ProcLibReportPrettyNames.Ellipsis + ProcLibReportPrettyNames.BinaryClose)
                );
            else if (utf8)
                raw = Term.List(
                    Term.I('<'),
                    Term.I('<'),
                    quoted,
                    Term.String(ProcLibReportPrettyNames.Utf8Suffix + ProcLibReportPrettyNames.BinaryClose)
                );
            else
                raw = Term.List(
                    Term.I('<'),
                    Term.I('<'),
                    quoted,
                    Term.I('>'),
                    Term.I('>')
                );

            var printed = ProcLibReportPrettyNode.Leaf(rendered, truncated ? 3 : 0, raw);

            return truncated ? WithMore(printed, (nextBudget, addedDepth) => BuildBinary(
                binary,
                AddDepth(depth, addedDepth),
                nextBudget,
                latin1
            )) : printed;
        }
        int originalDepth = depth;
        var elements = new List<ProcLibReportPrettyNode>();
        ProcLibReportPrettyNode? tail = null;
        budget = Subtract(budget, ProcLibReportPrettyParameters.BinaryDelimiters);
        for (int index = 0; index < bytes.Length; index++)
        {
            if (depth == 1 || budget == 0)
            {
                tail = Dots();
                break;
            }
            int bits = index + 1 == bytes.Length && !binary.IsBinary ? binary.BitLength % ProcLibReportPrettyParameters.BitsPerByte : ProcLibReportPrettyParameters.BitsPerByte;
            int value = bits == ProcLibReportPrettyParameters.BitsPerByte ? bytes[index] : bytes[index] >> (ProcLibReportPrettyParameters.BitsPerByte - bits);
            string digits = value.ToString(CultureInfo.InvariantCulture);
            string segment = bits == ProcLibReportPrettyParameters.BitsPerByte ? digits : digits + ProcLibReportPrettyNames.BitSizeSeparator + bits.ToString(CultureInfo.InvariantCulture);
            elements.Add(ProcLibReportPrettyNode.Leaf(segment));
            if (index + 1 < bytes.Length)
            {
                budget = Subtract(budget, digits.Length + 1);
                depth = Decrement(depth);
            }
        }

        var node = Container(
            ProcLibReportPrettyKind.Binary,
            elements,
            tail,
            ProcLibReportPrettyParameters.BinaryDelimiters
        );

        return node.Dots == 0 ? node : WithMore(node, (nextBudget, addedDepth) => BuildBinary(
            binary,
            AddDepth(originalDepth, addedDepth),
            nextBudget,
            latin1
        ));
    }

    private bool TryBinaryString(
        byte[] bytes,
        int maximum,
        int depth,
        bool latin1,
        out string? text,
        out bool utf8,
        out bool truncated
    )
    {
        var output = new StringBuilder();
        int offset = 0;
        int characters = 0;
        bool nonAscii = false;
        if (!latin1)
        {
            while (offset < bytes.Length && characters < maximum)
            {
                OperationStatus status = Rune.DecodeFromUtf8(bytes.AsSpan(offset), out Rune rune, out int consumed);
                if (status != OperationStatus.Done)
                {
                    latin1 = true;
                    break;
                }
                if (!Printable(rune.Value, !unicodePrintable))
                    break;

                output.Append(rune.ToString());
                offset += consumed;
                characters++;
                nonAscii |= rune.Value > 0x7f;
            }
        }
        if (latin1)
        {
            output.Clear();
            offset = 0;
            characters = 0;
            nonAscii = false;
            while (offset < bytes.Length && characters < maximum && Printable(bytes[offset], true))
            {
                output.Append((char)bytes[offset++]);
                characters++;
            }
        }
        text = output.ToString();
        utf8 = nonAscii;
        truncated = offset != bytes.Length;

        return !truncated || depth > 0 && characters >= depth;
    }
}

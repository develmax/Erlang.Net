namespace Erlang;

internal static class StringTrim
{
    internal static async ValueTask<Term> Trailing(ProcessContext context, Term input, HashSet<Term> separators)
    {
        var pending = new List<(Term Character, bool Separator, bool Raw)>();
        HashSet<Term> firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term result;
        while (true)
        {
            if (input is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                pending.Add((raw.Head, false, true));
                input = raw.Tail;
                continue;
            }
            if (input is BitString || input is Cons { Head: BitString })
            {
                result = await BinaryTrailing(context, input, separators);
                break;
            }
            Cons? next = await StringUnicode.Next(context, input, true);
            if (next is null)
            {
                result = Nil.Value;
                break;
            }
            pending.Add((next.Head, separators.Contains(next.Head), false));
            input = next.Tail;
            await context.ReduceAsync();
        }
        for (int index = pending.Count - 1; index >= 0; index--)
        {
            var entry = pending[index];
            if (entry.Separator && StringUnicode.IsEmpty(result))
            {
                result = Nil.Value;
            }
            else
            {
                result = entry.Raw ? new Cons(entry.Character, result) : StringUnicode.AppendCharacter(entry.Character, result);
            }
        }

        return result;
    }

    private static async ValueTask<Term> BinaryTrailing(ProcessContext context, Term input, HashSet<Term> separators)
    {
        bool list = input is Cons;
        BitString binary = list ? (BitString)((Cons)input).Head : (BitString)input;
        Term continuation = list ? ((Cons)input).Tail : Nil.Value;
        int start = 0;
        while (true)
        {
            var first = await StringSeparatorScan.Scan(
                context,
                binary.SliceBits(start * 8, binary.BitLength - start * 8),
                continuation,
                separators,
                true
            );
            if (!first.Found)
            {
                return list ? StringPatternSearch.Stack(binary, await Trailing(context, continuation, separators)) : binary;
            }
            var at = (Cons)first.Remainder;
            var separatorStart = (BitString)at.Head;
            var inverse = await StringSeparatorScan.Scan(
                context,
                separatorStart,
                at.Tail,
                separators,
                false
            );
            if (!inverse.Found)
            {
                Term tail = list ? await Trailing(context, inverse.Remainder, separators) : Nil.Value;
                if (StringUnicode.IsEmpty(tail))
                {
                    return binary.SliceBits(0, binary.BitLength - separatorStart.BitLength);
                }
                Term used = await StringSeparatorScan.UsedPrefix(context, continuation, inverse.Remainder);

                return StringPatternSearch.Stack(binary, StringPatternSearch.Stack(used, tail));
            }
            var nonSeparator = (Cons)inverse.Remainder;
            start = (binary.BitLength - ((BitString)nonSeparator.Head).BitLength) / 8;
            continuation = nonSeparator.Tail;
            await context.ReduceAsync();
        }
    }
}

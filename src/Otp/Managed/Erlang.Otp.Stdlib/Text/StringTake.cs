namespace Erlang;

internal static class StringTake
{
    internal static async ValueTask<Term> Leading(
        ProcessContext context,
        Term input,
        HashSet<Term> separators,
        bool complement
    )
    {
        var buffer = new StringBinarySearchBuffer([]);
        Term reverse = Nil.Value;
        HashSet<Term> firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        while (true)
        {
            if (complement && input is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                reverse = new Cons(raw.Head, reverse);
                input = raw.Tail;
                continue;
            }
            bool binaryList = input is Cons { Head: BitString };
            BitString? binary = binaryList ? (BitString)((Cons)input).Head : input as BitString;
            if (binary is not null)
            {
                Term continuation = binaryList ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.Scan(
                    context,
                    binary,
                    continuation,
                    separators,
                    complement,
                    buffer
                );
                if (!scan.Found)
                {
                    if (!binaryList)
                    {
                        return Term.Tuple(StringPatternSearch.BinaryToken(binary, reverse), new BitString([]));
                    }
                    Term used = await StringSeparatorScan.UsedPrefix(context, continuation, scan.Remainder);
                    Term combined = await context.Runtime.Modules.Call(
                        context,
                        UnicodeNativeNames.Module,
                        UnicodeNativeNames.ToBinary,
                        new Cons(binary, used)
                    );
                    reverse = new Cons(combined, reverse);
                    input = scan.Remainder;
                    continue;
                }
                var at = (Cons)scan.Remainder;
                var remainder = (BitString)at.Head;
                int keep = binary.BitLength - remainder.BitLength;

                return Term.Tuple(StringPatternSearch.BinaryToken(binary.SliceBits(0, keep), reverse), binaryList ? scan.Remainder : remainder);
            }
            Cons? next = await StringUnicode.Next(context, input, true);
            if (next is null)
            {
                return Term.Tuple(StringPatternSearch.Reverse(reverse), Nil.Value);
            }
            if (separators.Contains(next.Head) == complement)
            {
                return Term.Tuple(StringPatternSearch.Reverse(reverse), input);
            }
            IEnumerable<Term> characters = next.Head is Integer ? [next.Head] : Cons.Items(next.Head);
            foreach (Term character in characters)
            {
                reverse = new Cons(character, reverse);
            }
            input = next.Tail;
            await context.ReduceAsync();
        }
    }

    internal static async ValueTask<Term> Trailing(
        ProcessContext context,
        Term input,
        HashSet<Term> separators,
        bool complement
    )
    {
        // Explicit continuations keep long flat lists off the CLR call stack.
        var pending = new List<(Term Character, bool Accepted, bool Raw)>();
        HashSet<Term> firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term tail = input;
        TupleTerm result;
        while (true)
        {
            if (!complement && tail is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                pending.Add((raw.Head, false, true));
                tail = raw.Tail;
                continue;
            }
            if (tail is BitString || tail is Cons { Head: BitString })
            {
                result = (TupleTerm)await BinaryTrailing(
                    context,
                    tail,
                    separators,
                    complement
                );
                break;
            }
            Cons? next = await StringUnicode.Next(context, tail, true);
            if (next is null)
            {
                result = Term.Tuple(Nil.Value, Nil.Value);
                break;
            }
            pending.Add((next.Head, separators.Contains(next.Head) != complement, false));
            tail = next.Tail;
            await context.ReduceAsync();
        }
        for (int index = pending.Count - 1; index >= 0; index--)
        {
            var entry = pending[index];
            bool goesToTail = entry.Accepted && StringUnicode.IsEmpty(result.Items[0]);
            result = goesToTail
                ? Term.Tuple(result.Items[0], StringUnicode.AppendCharacter(entry.Character, result.Items[1]))
                : Term.Tuple(
                    entry.Raw ? new Cons(entry.Character, result.Items[0])
                    : StringUnicode.AppendCharacter(entry.Character, result.Items[0]),
                    result.Items[1]
                );
        }

        return result;
    }

    private static async ValueTask<Term> BinaryTrailing(
        ProcessContext context,
        Term input,
        HashSet<Term> separators,
        bool complement
    )
    {
        bool list = input is Cons;
        BitString binary = list ? (BitString)((Cons)input).Head : (BitString)input;
        Term continuation = list ? ((Cons)input).Tail : Nil.Value;
        int start = 0;
        var buffer = new StringBinarySearchBuffer([]);
        while (true)
        {
            var first = await StringSeparatorScan.Scan(
                context,
                binary.SliceBits(start * 8, binary.BitLength - start * 8),
                continuation,
                separators,
                !complement,
                buffer
            );
            if (!first.Found)
            {
                if (!list)
                {
                    return Term.Tuple(binary, new BitString([]));
                }
                Term used = await StringSeparatorScan.UsedPrefix(context, continuation, first.Remainder);
                var pair = (TupleTerm)await Trailing(
                    context,
                    first.Remainder,
                    separators,
                    complement
                );
                Term combined = await context.Runtime.Modules.Call(
                    context,
                    UnicodeNativeNames.Module,
                    UnicodeNativeNames.ToBinary,
                    new Cons(binary, used)
                );

                return Term.Tuple(StringPatternSearch.Stack(combined, pair.Items[0]), pair.Items[1]);
            }
            var at = (Cons)first.Remainder;
            var separatorStart = (BitString)at.Head;
            var inverse = await StringSeparatorScan.Scan(
                context,
                separatorStart,
                at.Tail,
                separators,
                complement,
                buffer
            );
            if (!inverse.Found)
            {
                var pair = list ? (TupleTerm)await Trailing(
                    context,
                    inverse.Remainder,
                    separators,
                    complement
                )
                    : Term.Tuple(Nil.Value, Nil.Value);
                Term used = list ? await StringSeparatorScan.UsedPrefix(context, continuation, inverse.Remainder) : Nil.Value;
                if (StringUnicode.IsEmpty(pair.Items[0]))
                {
                    int keep = binary.BitLength - separatorStart.BitLength;
                    Term suffix = list ? StringPatternSearch.Stack(StringPatternSearch.Stack(binary.SliceBits(keep, binary.BitLength - keep), used), pair.Items[1])
                        : binary.SliceBits(keep, binary.BitLength - keep);

                    return Term.Tuple(binary.SliceBits(0, keep), suffix);
                }
                Term combined = await context.Runtime.Modules.Call(
                    context,
                    UnicodeNativeNames.Module,
                    UnicodeNativeNames.ToBinary,
                    new Cons(binary, used)
                );

                return Term.Tuple(StringPatternSearch.Stack(combined, pair.Items[0]), pair.Items[1]);
            }
            var nonSeparator = (Cons)inverse.Remainder;
            start = (binary.BitLength - ((BitString)nonSeparator.Head).BitLength) / 8;
            continuation = nonSeparator.Tail;
            await context.ReduceAsync();
        }
    }
}

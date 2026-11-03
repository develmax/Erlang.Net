using System.Text;

namespace Erlang;

internal static class StringPatternSearch
{
    internal static Term Reverse(Term value)
    {
        if (value is Cons { Head: BitString binary, Tail: Nil })
        {
            return binary;
        }

        return Cons.From(Cons.Items(value).Reverse());
    }

    internal static Term Stack(Term first, Term tail)
    {
        if (tail is Nil)
        {
            return first;
        }
        if (first is Nil or BitString { BitLength: 0 })
        {
            return tail;
        }

        return new Cons(first, tail);
    }

    internal static Term BinaryToken(BitString binary, Term reverse)
    {
        if (reverse is Nil)
        {
            return binary;
        }
        if (reverse is Cons { Head: Integer cp, Tail: Nil })
        {
            byte[] prefix = Encoding.UTF8.GetBytes(char.ConvertFromUtf32((int)cp.Value));

            return new BitString(prefix.Concat(binary.ToArray()).ToArray());
        }
        if (binary.BitLength == 0)
        {
            return Cons.From(Cons.Items(reverse).Reverse());
        }

        return Term.List(Cons.From(Cons.Items(reverse).Reverse()), binary);
    }

    internal static async ValueTask<Term?> Prefix(ProcessContext context, Term input, IReadOnlyList<Term> needle)
    {
        Term tail = input;
        for (int index = 0; index < needle.Count; index++)
        {
            Term result = await StringUnicode.Utility(context, index == needle.Count - 1 ? StringUnicodeDependencyNames.Grapheme : StringUnicodeDependencyNames.Codepoint, tail);
            if (result is not Cons next || !next.Head.Equals(needle[index]))
            {
                return null;
            }
            tail = next.Tail;
        }

        return tail;
    }

    private static async ValueTask<StringBinaryMatch> BinarySearch(
        ProcessContext context,
        BitString binary,
        int start,
        Term continuation,
        IReadOnlyList<Term> needle
    )
    {
        byte[] bytes = binary.ToArray();
        string text = string.Concat((continuation is Nil ? needle : needle.Take(1)).Cast<Integer>().Select(cp => char.ConvertFromUtf32((int)cp.Value)));
        byte[] pattern = Encoding.UTF8.GetBytes(text);
        int position = start;
        while (position <= bytes.Length - pattern.Length)
        {
            int relative = bytes.AsSpan(position).IndexOf(pattern);
            if (relative < 0)
            {
                break;
            }
            int at = position + relative;
            BitString suffix = binary.SliceBits(at * 8, binary.BitLength - at * 8);
            Term candidate = continuation is Nil ? suffix : new Cons(suffix, continuation);
            Term? after = await Prefix(context, candidate, needle);
            if (after is not null)
            {
                return new StringBinaryMatch(
                    true,
                    binary.SliceBits(0, at * 8),
                    candidate,
                    after is Nil ? new BitString([]) : after,
                    at,
                    continuation
                );
            }
            Cons? next = await StringUnicode.Next(context, suffix, continuation is not Nil);
            if (next is null)
            {
                break;
            }
            if (next.Tail is BitString remainder)
            {
                position = bytes.Length - remainder.BitLength / 8;
            }
            else if (continuation is Nil)
            {
                break;
            }
            else
            {
                return new StringBinaryMatch(
                    false,
                    new BitString([]),
                    Nil.Value,
                    Nil.Value,
                    at,
                    Stack(new Cons(next.Head, next.Tail), continuation)
                );
            }
            await context.ReduceAsync();
        }

        return new StringBinaryMatch(
            false,
            new BitString([]),
            Nil.Value,
            Nil.Value,
            bytes.Length,
            continuation
        );
    }

    internal static async ValueTask<Term> Find(
        ProcessContext context,
        Term input,
        IReadOnlyList<Term> needle,
        bool trailing
    )
    {
        Term result = Term.A(StringTextAtoms.NoMatch);
        while (true)
        {
            if (input is Cons { Head: BitString binary } binaryCell)
            {
                StringBinaryMatch match = await BinarySearch(
                    context,
                    binary,
                    0,
                    binaryCell.Tail,
                    needle
                );
                if (match.Matched)
                {
                    result = match.At is BitString ? new Cons(match.At, binaryCell.Tail) : match.At;
                    if (!trailing)
                    {
                        return result;
                    }
                    input = (await StringUnicode.Next(context, result, true))!.Tail;
                }
                else
                {
                    input = match.Continuation;
                }
                continue;
            }
            if (input is BitString bin)
            {
                StringBinaryMatch match = await BinarySearch(
                    context,
                    bin,
                    0,
                    Nil.Value,
                    needle
                );
                if (!match.Matched)
                {
                    return result;
                }
                result = match.At;
                if (!trailing)
                {
                    return result;
                }
                input = (await StringUnicode.Next(context, match.At, false))!.Tail;
                continue;
            }
            if (input is not (Cons or Nil))
            {
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), input));
            }
            Cons? next = input is Cons { Head: Integer } direct ? direct : await StringUnicode.Next(context, input, false);
            if (next is null)
            {
                return result;
            }
            if (next.Head.Equals(needle[0]) && await Prefix(context, input, needle) is not null)
            {
                result = input;
                if (!trailing)
                {
                    return result;
                }
            }
            input = next.Tail;
            await context.ReduceAsync();
        }
    }

    internal static async ValueTask<Term> Split(
        ProcessContext context,
        Term original,
        IReadOnlyList<Term> needle,
        string where
    )
    {
        Term input = original;
        Term current = Nil.Value;
        Term accumulated = Nil.Value;
        int start = 0;
        while (true)
        {
            bool binaryList = input is Cons { Head: BitString };
            BitString? binary = binaryList ? (BitString)((Cons)input).Head : input as BitString;
            if (binary is not null)
            {
                Term continuation = binaryList ? ((Cons)input).Tail : Nil.Value;
                StringBinaryMatch match = await BinarySearch(
                    context,
                    binary,
                    start,
                    continuation,
                    needle
                );
                if (!match.Matched)
                {
                    if (!binaryList)
                    {
                        current = new Cons(binary.SliceBits(start * 8, binary.BitLength - start * 8), current);
                        break;
                    }
                    current = new Cons(binary.SliceBits(0, match.ConsumedBytes * 8), current);
                    input = match.Continuation;
                    start = 0;
                    continue;
                }
                Term before = binaryList
                    ? match.Before.BitLength == 0 ? current : new Cons(match.Before, current)
                    : new Cons(match.Before, current);
                Term head = !binaryList && where == StringTextAtoms.Trailing ? BinaryToken(match.Before, current) : Reverse(before);
                if (where == StringTextAtoms.Leading)
                {
                    return Term.List(head, match.After);
                }
                if (where == StringTextAtoms.Trailing)
                {
                    accumulated = Term.List(head, match.After);
                    Term at = match.At is Cons cell ? cell.Head : match.At;
                    var remainder = (BitString)(await StringUnicode.Next(context, at, false))!.Tail;
                    start = binary.BitLength / 8 - remainder.BitLength / 8;
                }
                else
                {
                    if (binaryList)
                    {
                        accumulated = new Cons(head, accumulated);
                        input = match.After;
                        start = 0;
                    }
                    else
                    {
                        var after = (BitString)match.After;
                        int next = binary.BitLength / 8 - after.BitLength / 8;
                        BitString keep = match.Before.SliceBits(start * 8, match.Before.BitLength - start * 8);
                        accumulated = new Cons(Reverse(new Cons(keep, current)), accumulated);
                        start = next;
                    }
                    current = Nil.Value;
                }
                continue;
            }
            if (input is not (Cons or Nil))
            {
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), input));
            }
            Cons? nextCell = input is Cons { Head: Integer } direct ? direct : await StringUnicode.Next(context, input, false);
            if (nextCell is null)
            {
                break;
            }
            Term? rest = nextCell.Head.Equals(needle[0]) ? await Prefix(context, input, needle) : null;
            if (rest is not null)
            {
                if (where == StringTextAtoms.Leading)
                {
                    return Term.List(Reverse(current), rest);
                }
                if (where == StringTextAtoms.Trailing)
                {
                    accumulated = Term.List(Reverse(current), rest);
                }
                else
                {
                    accumulated = new Cons(Reverse(current), accumulated);
                    current = Nil.Value;
                    input = rest;
                    start = 0;
                    continue;
                }
            }
            current = new Cons(nextCell.Head, current);
            input = nextCell.Tail;
            start = 0;
            await context.ReduceAsync();
        }
        if (accumulated is Nil)
        {
            return Term.List(original);
        }

        return where == StringTextAtoms.Trailing ? accumulated : Cons.From(Cons.Items(new Cons(Reverse(current), accumulated)).Reverse());
    }
}

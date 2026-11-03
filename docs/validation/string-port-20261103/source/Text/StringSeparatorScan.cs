using System.Text;

namespace Erlang;

internal static class StringSeparatorScan
{
    internal static async ValueTask<HashSet<Term>> FirstCodepoints(ProcessContext context, HashSet<Term> separators)
    {
        var result = new HashSet<Term>();
        foreach (Term separator in separators)
        {
            result.Add(separator is Integer ? separator : (await StringUnicode.Next(context, separator, false))!.Head);
        }

        return result;
    }

    internal static async ValueTask<(bool Found, Term Remainder)> Scan(
        ProcessContext context,
        BitString binary,
        Term continuation,
        HashSet<Term> separators,
        bool member
    )
    {
        var patterns = new List<byte[]>();
        foreach (Term separator in separators)
        {
            Term first = separator is Integer ? separator : (await StringUnicode.Next(context, separator, false))!.Head;
            patterns.Add(Encoding.UTF8.GetBytes(char.ConvertFromUtf32((int)((Integer)first).Value)));
        }
        BitString rest = binary;
        while (rest.BitLength > 0)
        {
            if (member)
            {
                byte[] bytes = rest.ToArray();
                int position = -1;
                foreach (byte[] pattern in patterns)
                {
                    int candidate = bytes.AsSpan().IndexOf(pattern);
                    if (candidate >= 0 && (position < 0 || candidate < position))
                    {
                        position = candidate;
                    }
                }
                if (position < 0)
                {
                    return (false, continuation);
                }
                rest = rest.SliceBits(position * 8, rest.BitLength - position * 8);
            }
            else
            {
                byte[] bytes = rest.ToArray();
                if (Rune.DecodeFromUtf8(bytes, out _, out _) != System.Buffers.OperationStatus.Done)
                {
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), rest));
                }
            }
            Term at = continuation is Nil ? rest : new Cons(rest, continuation);
            Term value = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, at);
            if (value is not Cons next)
            {
                if (!member && separators.Count == 1)
                {
                    return (true, new Cons(rest, continuation));
                }

                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value));
            }
            if (separators.Contains(next.Head) == member)
            {
                return (true, new Cons(rest, continuation));
            }
            if (next.Tail is BitString remainder && continuation is Nil)
            {
                rest = remainder;
            }
            else if (next.Tail is Cons { Head: BitString remaining } cell && cell.Tail.Equals(continuation))
            {
                rest = remaining;
            }
            else
            {
                return (false, next.Tail);
            }
            await context.ReduceAsync();
        }

        return (false, continuation);
    }

    internal static async ValueTask<Term> UsedPrefix(ProcessContext context, Term original, Term continuation)
    {
        if (StringUnicode.IsEmpty(continuation))
        {
            return original;
        }
        List<Term> all = await StringUnicode.Elements(context, original, false);
        List<Term> remaining = await StringUnicode.Elements(context, continuation, false);

        return Cons.From(all.Take(all.Count - remaining.Count));
    }
}

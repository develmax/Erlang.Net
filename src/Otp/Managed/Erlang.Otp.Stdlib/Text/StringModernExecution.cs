using System.Numerics;
using System.Text;

namespace Erlang;

internal static class StringModernExecution
{
    internal static bool AsciiPair(Term input, out Cons first)
    {
        if (input is Cons { Head: Integer a, Tail: Cons { Head: Integer b } } cell
            && a.Value >= 0 && a.Value < 256 && b.Value >= 0 && b.Value < 256 && a.Value != '\r')
        {
            first = cell;

            return true;
        }
        first = null!;

        return false;
    }

    internal static bool Decode(BitString input, out Integer codepoint, out BitString tail)
    {
        int lookahead = Math.Min(input.BitLength, StringDecodingLimits.MaximumUtf8ScalarBytes * UnicodeNativeNames.BitsPerByte);
        if (input.IsBinary && Rune.DecodeFromUtf8(input.SliceBits(0, lookahead).ToArray(), out Rune rune, out int bytes) == System.Buffers.OperationStatus.Done)
        {
            codepoint = Term.I(rune.Value);
            tail = input.SliceBits(bytes * UnicodeNativeNames.BitsPerByte, input.BitLength - bytes * UnicodeNativeNames.BitsPerByte);

            return true;
        }
        codepoint = null!;
        tail = null!;

        return false;
    }

    internal static async ValueTask<Term> LengthOrReverse(ProcessContext context, Term input, bool reverse)
    {
        int count = 0;
        Term accumulated = Nil.Value;
        if (input is BitString binary && Decode(binary, out Integer cp, out BitString rest))
        {
            string helper = reverse ? StringSourceNames.ReverseBinary : StringSourceNames.LengthBinary;
            while (true)
            {
                if (Decode(rest, out Integer nextCp, out BitString nextRest)
                    && cp.Value >= 0 && cp.Value < 256 && nextCp.Value < 256 && cp.Value != '\r')
                {
                    accumulated = reverse ? new Cons(cp, accumulated) : accumulated;
                    count++;
                    cp = nextCp;
                    rest = nextRest;
                    await context.ReduceAsync();
                    continue;
                }
                Term cluster;
                int gcLine = reverse ? StringModernSource.ReverseBinaryGc : StringModernSource.LengthBinaryGc;
                using (StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    gcLine
                ))
                {
                    cluster = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, new Cons(cp, rest));
                }
                if (cluster is not Cons found)
                {
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), cluster),
                        helper,
                        3,
                        gcLine
                    );
                }
                accumulated = reverse ? new Cons(found.Head, accumulated) : accumulated;
                count++;
                Term next;
                using (StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    reverse ? StringModernSource.ReverseBinaryCp : StringModernSource.LengthBinaryCp
                ))
                {
                    next = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, found.Tail);
                }
                if (next is Nil)
                {
                    return reverse ? accumulated : Term.I(count);
                }
                if (next is Cons { Head: Integer point, Tail: BitString remaining })
                {
                    cp = point;
                    rest = remaining;
                    await context.ReduceAsync();
                    continue;
                }
                var error = (TupleTerm)next;
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), error.Items[1]),
                    helper,
                    3,
                    reverse ? StringModernSource.ReverseBinaryError : StringModernSource.LengthBinaryError
                );
            }
        }
        while (true)
        {
            if (AsciiPair(input, out Cons first))
            {
                accumulated = reverse ? new Cons(first.Head, accumulated) : accumulated;
                input = first.Tail;
                count++;
                await context.ReduceAsync();
                continue;
            }
            Cons? next = await StringUnicode.Next(
                context,
                input,
                true,
                reverse ? StringModernSource.Reverse : StringModernSource.Length
            );
            if (next is null)
            {
                return reverse ? accumulated : Term.I(count);
            }
            accumulated = reverse ? new Cons(next.Head, accumulated) : accumulated;
            input = next.Tail;
            count++;
            await context.ReduceAsync();
        }
    }

    internal static async ValueTask<Term> Graphemes(ProcessContext context, Term input)
    {
        var values = new List<Term>();
        IDisposable? continuation = null;
        try
        {
            while (await StringUnicode.Next(
                context,
                input,
                true,
                StringModernSource.Graphemes
            ) is Cons next)
            {
                values.Add(next.Head);
                input = next.Tail;
                continuation ??= StringSourceTrace.Scope(
                    context,
                    StringFunctionNames.ToGraphemes,
                    1,
                    StringModernSource.GraphemesRecursive
                );
                await context.ReduceAsync();
            }

            return Cons.From(values);
        }
        finally
        {
            continuation?.Dispose();
        }
    }

    internal static async ValueTask<Term> Pad(ProcessContext context, IReadOnlyList<Term> a)
    {
        Term input = a[0];
        Term direction = a.Count > 2 ? a[2] : Term.A(StringTextAtoms.Trailing);
        Term fill = a.Count == 4 ? a[3] : Term.I(' ');
        if (a[1] is not Integer length || direction is not Atom { Name: StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.Both } which)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Pad,
                [input, a[1], direction, fill],
                StringModernSource.PadHead
            );
        }
        Integer actual;
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.Pad,
            4,
            which.Name switch
            {
                StringTextAtoms.Leading => StringModernSource.PadLeadingLength,
                StringTextAtoms.Trailing => StringModernSource.PadTrailingLength,
                _ => StringModernSource.PadBothLength
            }
        ))
        {
            actual = (Integer)await LengthOrReverse(context, input, false);
        }
        int count = StringLegacy.Count(new Integer(BigInteger.Max(0, length.Value - actual.Value)));
        if (which.Name == StringTextAtoms.Both)
        {
            Term half = Cons.From(Enumerable.Repeat(fill, count / 2));

            return Cons.From([half, input, half], count % 2 == 0 ? Nil.Value : Term.List(fill));
        }
        Term padding = Cons.From(Enumerable.Repeat(fill, count));

        return which.Name == StringTextAtoms.Leading ? Term.List(padding, input) : new Cons(input, padding);
    }

    internal static async ValueTask<Term> Slice(ProcessContext context, IReadOnlyList<Term> a)
    {
        Term input = a[0];
        Term limit = a.Count == 2 ? Term.A(StringTextAtoms.Infinity) : a[2];
        if (limit.Equals(Term.I(0)))
        {
            return StringUnicode.Empty(input);
        }
        if (a[1] is not Integer start || start.Value < 0
            || limit is not Integer { Value.Sign: > 0 } && !limit.Equals(Term.A(StringTextAtoms.Infinity)))
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Slice,
                a,
                a.Count == 2 ? StringModernSource.SliceTwoHead : StringModernSource.SliceHead
            );
        }
        Term tail;
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.Slice,
            a.Count,
            a.Count == 2 ? StringModernSource.SliceTwoDropCall : limit.Equals(Term.A(StringTextAtoms.Infinity))
                ? StringModernSource.SliceInfinityDropCall : StringModernSource.SliceDropCall
        ))
        {
            tail = await Drop(context, input, start.Value);
        }
        if (limit.Equals(Term.A(StringTextAtoms.Infinity)))
        {
            return tail is Nil && input is BitString ? StringUnicode.Empty(input) : tail;
        }
        if (tail is Nil && input is BitString)
        {
            return StringUnicode.Empty(input);
        }
        if (tail is BitString { IsBinary: true } binary)
        {
            return await KeepBinary(context, binary, ((Integer)limit).Value);
        }
        if (tail is not (Cons or Nil))
        {
            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.SliceTrail,
                [tail, limit],
                StringModernSource.SliceTrailHead
            );
        }
        var codepoints = new List<Term>();
        var pending = new List<IDisposable>();
        BigInteger remaining = ((Integer)limit).Value;
        try
        {
            while (remaining > 0)
            {
                bool ascii = AsciiPair(tail, out Cons direct);
                Cons? next = ascii ? direct : await StringUnicode.Next(
                    context,
                    tail,
                    true,
                    StringModernSource.SliceList
                );
                if (next is null)
                {
                    break;
                }
                codepoints.AddRange(next.Head is Integer ? [next.Head] : Cons.Items(next.Head));
                tail = next.Tail;
                remaining--;
                pending.Add(StringSourceTrace.Scope(
                    context,
                    StringSourceNames.SliceList,
                    2,
                    ascii ? StringModernSource.SliceListAsciiRecursive : StringModernSource.SliceListRecursive
                ));
                await context.ReduceAsync();
            }

            return Cons.From(codepoints);
        }
        finally
        {
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                pending[index].Dispose();
            }
        }
    }

    private static async ValueTask<Term> Drop(ProcessContext context, Term input, BigInteger count)
    {
        bool binaryRoot = input is BitString;
        while (count > 0)
        {
            if (binaryRoot && input is BitString binary && Decode(binary, out Integer cp, out BitString rest))
            {
                while (true)
                {
                    if (count > 1 && Decode(rest, out Integer next, out BitString following)
                        && cp.Value < 256 && next.Value < 256 && cp.Value != '\r')
                    {
                        cp = next;
                        rest = following;
                        count--;
                        await context.ReduceAsync();
                        continue;
                    }
                    Term cluster;
                    using (StringSourceTrace.Scope(
                        context,
                        StringSourceNames.SliceDropBinary,
                        3,
                        StringModernSource.SliceBinaryDropGc
                    ))
                    {
                        cluster = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, new Cons(cp, rest));
                    }
                    if (cluster is not Cons found)
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), cluster),
                            StringSourceNames.SliceDropBinary,
                            3,
                            StringModernSource.SliceBinaryDropGc
                        );
                    }
                    if (count == 1)
                    {
                        return found.Tail;
                    }
                    Term point;
                    using (StringSourceTrace.Scope(
                        context,
                        StringSourceNames.SliceDropBinary,
                        3,
                        StringModernSource.SliceBinaryDropCp
                    ))
                    {
                        point = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, found.Tail);
                    }
                    if (point is Nil)
                    {
                        return new BitString([]);
                    }
                    if (point is Cons { Head: Integer first, Tail: BitString remaining })
                    {
                        cp = first;
                        rest = remaining;
                        count--;
                        await context.ReduceAsync();
                        continue;
                    }
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), ((TupleTerm)point).Items[1]),
                        StringSourceNames.SliceDropBinary,
                        3,
                        StringModernSource.SliceBinaryDropError
                    );
                }
            }
            Cons? cell = AsciiPair(input, out Cons direct) ? direct : await StringUnicode.Next(
                context,
                input,
                true,
                StringModernSource.SliceDrop
            );
            if (cell is null)
            {
                return Nil.Value;
            }
            input = cell.Tail;
            count--;
            await context.ReduceAsync();
        }

        return input;
    }

    private static async ValueTask<Term> KeepBinary(ProcessContext context, BitString binary, BigInteger count)
    {
        if (binary.BitLength == 0)
        {
            return binary;
        }
        if (!Decode(binary, out Integer cp, out BitString rest))
        {
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), binary),
                StringSourceNames.SliceTrail,
                2,
                StringModernSource.SliceTrailError
            );
        }
        using var call = StringSourceTrace.Scope(
            context,
            StringSourceNames.SliceTrail,
            2,
            StringModernSource.SliceTrailKeepCall
        );
        while (count > 0)
        {
            if (Decode(rest, out Integer next, out BitString following) && cp.Value < 256 && next.Value < 256 && cp.Value != '\r')
            {
                cp = next;
                rest = following;
                count--;
                await context.ReduceAsync();
                continue;
            }
            Term cluster;
            using (StringSourceTrace.Scope(
                context,
                StringSourceNames.SliceBinary,
                3,
                StringModernSource.SliceBinaryGc
            ))
            {
                cluster = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, new Cons(cp, rest));
            }
            if (cluster is not Cons found)
            {
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), cluster),
                    StringSourceNames.SliceBinary,
                    3,
                    StringModernSource.SliceBinaryGc
                );
            }
            Term point;
            using (StringSourceTrace.Scope(
                context,
                StringSourceNames.SliceBinary,
                3,
                StringModernSource.SliceBinaryCp
            ))
            {
                point = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, found.Tail);
            }
            if (point is Nil)
            {
                return binary;
            }
            if (point is not Cons { Head: Integer first, Tail: BitString remaining })
            {
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), ((TupleTerm)point).Items[1]),
                    StringSourceNames.SliceBinary,
                    3,
                    StringModernSource.SliceBinaryError
                );
            }
            cp = first;
            rest = remaining;
            count--;
        }
        int bytesLeft = rest.BitLength / UnicodeNativeNames.BitsPerByte + new Rune((int)cp.Value).Utf8SequenceLength;

        return binary.SliceBits(0, binary.BitLength - bytesLeft * UnicodeNativeNames.BitsPerByte);
    }
}

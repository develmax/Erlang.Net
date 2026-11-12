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

    internal static async ValueTask<Term?> Prefix(ProcessContext context, Term input, StringSearchPattern needle)
    {
        if (!needle.HasHead)
        {
            throw StringSourceTrace.Clause(
                context,
                StringSearchSource.Prefix,
                [input, needle.Original],
                StringSearchSource.PrefixHead
            );
        }
        Term tail = input;
        for (int index = 0; index < needle.Count; index++)
        {
            bool last = index == needle.Count - 1;
            Term result;
            using (StringSourceTrace.Scope(
                context,
                StringSearchSource.Prefix,
                2,
                last ? StringSearchSource.PrefixGc : StringSearchSource.PrefixCp
            ))
            {
                result = await StringUnicode.Utility(context, last ? StringUnicodeDependencyNames.Grapheme : StringUnicodeDependencyNames.Codepoint, tail);
            }
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
        Term input,
        int start,
        Term continuation,
        StringSearchPattern needle
    )
    {
        if (continuation is not Nil && !needle.HasHead)
        {
            throw StringSourceTrace.Clause(
                context,
                StringSearchSource.BinarySearch,
                [input, Term.I(start), continuation, needle.Original],
                StringSearchSource.BinarySearchHead
            );
        }
        Term compiled;
        using (StringSourceTrace.Scope(
            context,
            StringSearchSource.BinarySearch,
            4,
            continuation is Nil ? StringSearchSource.BinaryCompile : StringSearchSource.BinaryFirstCompile
        ))
        {
            Term encoded;
            if (continuation is Nil)
            {
                encoded = await context.Runtime.Modules.Call(
                    context,
                    UnicodeNativeNames.Module,
                    UnicodeNativeNames.ToBinary,
                    needle.Original
                );
            }
            else
            {
                try
                {
                    encoded = BeamBinaryConstructionValues.Utf8(needle[0]);
                }
                catch (ErlangException error)
                {
                    throw BeamBinaryConstructionValues.CaptureFailure(
                        context,
                        error,
                        1,
                        BeamBinaryNames.Utf8Segment,
                        needle[0]
                    );
                }
            }
            compiled = await context.Runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.CompilePattern,
                encoded
            );
        }
        if (input is not BitString { IsBinary: true } binary)
        {
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), input),
                continuation is Nil ? StringSearchSource.BinarySingle : StringSearchSource.BinaryMultiple,
                continuation is Nil ? 4 : 5,
                continuation is Nil ? StringSearchSource.BinaryBadMatch : StringSearchSource.BinaryMultipleBadMatch
            );
        }
        int byteLength = binary.BitLength / UnicodeNativeNames.BitsPerByte;
        int position = start;
        while (position <= byteLength)
        {
            Term found;
            using (StringSourceTrace.Scope(
                context,
                continuation is Nil ? StringSearchSource.BinarySingle : StringSearchSource.BinaryMultiple,
                continuation is Nil ? 4 : 5,
                continuation is Nil ? StringSearchSource.BinaryMatch : StringSearchSource.BinaryMultipleMatch
            ))
            {
                found = await context.Runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Match,
                    binary.SliceBits(position * UnicodeNativeNames.BitsPerByte, binary.BitLength - position * UnicodeNativeNames.BitsPerByte),
                    compiled
                );
            }
            if (found is not TupleTerm match)
            {
                break;
            }
            int relative = (int)((Integer)match.Items[0]).Value;
            int at = position + relative;
            BitString suffix = binary.SliceBits(at * 8, binary.BitLength - at * 8);
            Term candidate = continuation is Nil ? suffix : new Cons(suffix, continuation);
            Cons? cluster = null;
            if (continuation is not Nil)
            {
                Term gc;
                using (StringSourceTrace.Scope(
                    context,
                    StringSearchSource.BinaryMultiple,
                    5,
                    StringSearchSource.BinaryMultipleGc
                ))
                {
                    gc = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, suffix);
                }
                cluster = gc as Cons;
                if (cluster is null)
                {
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), gc),
                        StringSearchSource.BinaryMultiple,
                        5,
                        StringSearchSource.BinaryMultipleGc
                    );
                }
            }
            Term? after;
            using (StringSourceTrace.Scope(
                context,
                continuation is Nil ? StringSearchSource.BinarySingle : StringSearchSource.BinaryMultiple,
                continuation is Nil ? 4 : 5,
                continuation is Nil ? StringSearchSource.BinarySinglePrefix : StringSearchSource.BinaryMultiplePrefix
            ))
            {
                after = await Prefix(context, candidate, needle);
            }
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
            Cons? next = cluster;
            if (continuation is Nil)
            {
                if (!StringModernExecution.Decode(suffix, out Integer skipped, out BitString remaining))
                {
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), suffix),
                        StringSearchSource.BinarySingle,
                        4,
                        StringSearchSource.BinarySingleAdvance
                    );
                }
                next = new Cons(skipped, remaining);
            }
            if (next is null)
            {
                break;
            }
            if (next.Tail is BitString remainder)
            {
                position = byteLength - remainder.BitLength / 8;
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
            byteLength,
            continuation
        );
    }

    internal static async ValueTask<Term> Find(
        ProcessContext context,
        Term input,
        StringSearchPattern needle,
        bool trailing
    )
    {
        Term result = Term.A(StringTextAtoms.NoMatch);
        while (true)
        {
            if (!needle.HasHead)
            {
                bool listBinary = input is Cons { Head: BitString { IsBinary: true } };
                Term searchInput = listBinary ? ((Cons)input).Head : input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                StringBinaryMatch empty = await BinarySearchFor(
                    context,
                    searchInput,
                    0,
                    continuation,
                    needle,
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? listBinary ? StringSearchSource.FindTrailingListBinary : StringSearchSource.FindTrailingBinary
                        : listBinary ? StringSearchSource.FindLeadingListBinary : StringSearchSource.FindLeadingBinary
                );

                return empty.Matched ? empty.At : result;
            }
            if (input is Cons { Head: BitString { IsBinary: true } binary } binaryCell)
            {
                StringBinaryMatch match = await BinarySearchFor(
                    context,
                    binary,
                    0,
                    binaryCell.Tail,
                    needle,
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? StringSearchSource.FindTrailingListBinary : StringSearchSource.FindLeadingListBinary
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
            if (input is BitString { IsBinary: true } bin)
            {
                StringBinaryMatch match = await BinarySearchFor(
                    context,
                    bin,
                    0,
                    Nil.Value,
                    needle,
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? StringSearchSource.FindTrailingBinary : StringSearchSource.FindLeadingBinary
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
                using var call = StringSourceTrace.Scope(
                    context,
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? StringSearchSource.FindTrailingBinary : StringSearchSource.FindLeadingBinary
                );
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), input),
                    StringSearchSource.BinarySingle,
                    4,
                    StringSearchSource.BinaryBadMatch
                );
            }
            bool directInput = input is Cons { Head: Integer };
            Cons? next = directInput ? (Cons)input : await StringUnicode.Next(
                context,
                input,
                false,
                new StringSourceStep(
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? StringSearchSource.FindTrailingCp : StringSearchSource.FindLeadingCp,
                    trailing ? StringSearchSource.FindTrailingCp : StringSearchSource.FindLeadingCp,
                    true
                )
            );
            if (next is null)
            {
                return result;
            }
            Term? matched = null;
            if (next.Head.Equals(needle[0]))
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    trailing ? StringSearchSource.FindTrailing : StringSearchSource.FindLeading,
                    trailing ? 3 : 2,
                    trailing ? directInput ? StringSearchSource.FindTrailingPrefix : StringSearchSource.FindTrailingNestedPrefix
                        : directInput ? StringSearchSource.FindLeadingPrefix : StringSearchSource.FindLeadingNestedPrefix
                );
                matched = await Prefix(context, input, needle);
            }
            if (matched is not null)
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
        StringSearchPattern needle,
        Term whereTerm
    )
    {
        string where = whereTerm is Atom atom ? atom.Name : "";
        Term input = original;
        Term current = Nil.Value;
        Term accumulated = Nil.Value;
        int start = 0;
        while (true)
        {
            bool binaryList = input is Cons { Head: BitString { IsBinary: true } };
            if (!needle.HasHead && !binaryList)
            {
                throw StringSourceTrace.Clause(
                    context,
                    StringSearchSource.Split,
                    [input, needle.Original, Term.I(start), whereTerm, current, accumulated],
                    StringSearchSource.SplitHead
                );
            }
            BitString? binary = binaryList ? (BitString)((Cons)input).Head : input is BitString { IsBinary: true } binaryValue ? binaryValue : null;
            if (binary is not null)
            {
                Term continuation = binaryList ? ((Cons)input).Tail : Nil.Value;
                StringBinaryMatch match = await BinarySearchFor(
                    context,
                    binary,
                    start,
                    continuation,
                    needle,
                    StringSearchSource.Split,
                    6,
                    binaryList ? StringSearchSource.SplitListBinary : StringSearchSource.SplitBinary
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
                if (where is not (StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.All))
                {
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), whereTerm),
                        StringSearchSource.Split,
                        6,
                        binaryList ? StringSearchSource.SplitListDirection : StringSearchSource.SplitBinaryDirection
                    );
                }
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
                using var call = StringSourceTrace.Scope(
                    context,
                    StringSearchSource.Split,
                    6,
                    StringSearchSource.SplitBinary
                );
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), input),
                    StringSearchSource.BinarySingle,
                    4,
                    StringSearchSource.BinaryBadMatch
                );
            }
            bool directCell = input is Cons { Head: Integer };
            Cons? nextCell = directCell ? (Cons)input : await StringUnicode.Next(
                context,
                input,
                false,
                new StringSourceStep(
                    StringSearchSource.Split,
                    6,
                    StringSearchSource.SplitCp,
                    StringSearchSource.SplitCp,
                    true
                )
            );
            if (nextCell is null)
            {
                break;
            }
            Term? rest = null;
            if (nextCell.Head.Equals(needle[0]))
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    StringSearchSource.Split,
                    6,
                    directCell ? StringSearchSource.SplitPrefix : StringSearchSource.SplitNestedPrefix
                );
                rest = await Prefix(context, input, needle);
            }
            if (rest is not null)
            {
                if (where is not (StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.All))
                {
                    throw StringSourceTrace.Error(
                        context,
                        Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), whereTerm),
                        StringSearchSource.Split,
                        6,
                        directCell ? StringSearchSource.SplitScalarDirection : StringSearchSource.SplitNestedDirection
                    );
                }
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

    private static async ValueTask<StringBinaryMatch> BinarySearchFor(
        ProcessContext context,
        Term binary,
        int start,
        Term continuation,
        StringSearchPattern needle,
        string function,
        int arity,
        int line
    )
    {
        using var call = StringSourceTrace.Scope(
            context,
            function,
            arity,
            line
        );

        return await BinarySearch(
            context,
            binary,
            start,
            continuation,
            needle
        );
    }
}

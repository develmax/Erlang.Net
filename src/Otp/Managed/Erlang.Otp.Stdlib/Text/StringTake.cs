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
        Term reverse = Nil.Value;
        StringSeparatorSet pattern = StringSeparatorSet.From(separators);
        separators = pattern;
        string helper = complement ? StringSeparatorSource.TakeLeadingComplement : StringSeparatorSource.TakeLeading;
        HashSet<Term> firstCodepoints = complement ? await StringSeparatorScan.FirstCodepoints(context, separators) : new();
        while (true)
        {
            if (complement && input is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                reverse = new Cons(raw.Head, reverse);
                input = raw.Tail;
                await context.ReduceAsync();
                continue;
            }
            bool binaryList = input is Cons { Head: BitString { IsBinary: true } };
            BitString? binary = binaryList ? (BitString)((Cons)input).Head : input is BitString { IsBinary: true } binaryValue ? binaryValue : null;
            if (binary is not null)
            {
                Term continuation = binaryList ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.ScanFor(
                    context,
                    binary,
                    continuation,
                    separators,
                    complement,
                    helper,
                    3,
                    complement ? binaryList ? StringSeparatorSource.TakeComplementListBinary : StringSeparatorSource.TakeComplementBinary
                        : binaryList ? StringSeparatorSource.TakeLeadingListBinary : StringSeparatorSource.TakeLeadingBinary
                );
                if (!scan.Found)
                {
                    if (!binaryList)
                    {
                        return Term.Tuple(StringPatternSearch.BinaryToken(binary, reverse), new BitString([]));
                    }
                    Term used = await StringConsumedPrefix.Run(
                        context,
                        continuation,
                        scan.Remainder,
                        helper,
                        complement ? StringSeparatorSource.TakeComplementConsumedPrefix : StringSeparatorSource.TakeLeadingConsumedPrefix
                    );
                    Term combined = await context.Runtime.Modules.Call(
                        context,
                        UnicodeNativeNames.Module,
                        UnicodeNativeNames.ToBinary,
                        new Cons(binary, used)
                    );
                    reverse = new Cons(combined, reverse);
                    input = scan.Remainder;
                    await context.ReduceAsync();
                    continue;
                }
                var at = (Cons)scan.Remainder;
                var remainder = (BitString)at.Head;
                int keep = binary.BitLength - remainder.BitLength;

                return Term.Tuple(StringPatternSearch.BinaryToken(binary.SliceBits(0, keep), reverse), binaryList ? scan.Remainder : remainder);
            }
            if (input is not (Cons or Nil) || input is BitString { IsBinary: false })
            {
                throw StringSourceTrace.Clause(
                    context,
                    helper,
                    [input, complement ? await pattern.Pattern(context) : pattern.Original, reverse],
                    complement ? StringSeparatorSource.TakeComplementHead : StringSeparatorSource.TakeLeadingHead
                );
            }
            int gcLine = complement ? input is Cons { Head: Integer } ? StringSeparatorSource.TakeComplementScalarGc : StringSeparatorSource.TakeComplementGc
                : StringSeparatorSource.TakeLeadingGc;
            Cons? next = !complement && StringModernExecution.AsciiPair(input, out Cons direct) ? direct
                : await StringUnicode.Next(
                    context,
                    input,
                    true,
                    new StringSourceStep(
                        helper,
                        3,
                        gcLine,
                        gcLine,
                        true
                    )
                );
            if (next is null)
            {
                return Term.Tuple(StringPatternSearch.Reverse(reverse), Nil.Value);
            }
            bool member = complement ? separators.Contains(next.Head)
                : await pattern.Member(
                    context,
                    next.Head,
                    helper,
                    3,
                    StringModernExecution.AsciiPair(input, out _) ? StringSeparatorSource.TakeLeadingAsciiMember : StringSeparatorSource.TakeLeadingMember
                );
            if (member == complement)
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
        StringSeparatorSet pattern = StringSeparatorSet.From(separators);
        separators = pattern;
        string helper = complement ? StringSeparatorSource.TakeTrailingComplement : StringSeparatorSource.TakeTrailing;
        using var sourcePending = new StringSourceContinuations(context, helper, 3);
        HashSet<Term> firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term tail = input;
        TupleTerm result;
        while (true)
        {
            if (!complement && tail is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                pending.Add((raw.Head, false, true));
                sourcePending.Push(StringSeparatorSource.TakeTrailingRawRecursion);
                tail = raw.Tail;
                await context.ReduceAsync();
                continue;
            }
            if (tail is BitString { IsBinary: true } || tail is Cons { Head: BitString { IsBinary: true } })
            {
                result = (TupleTerm)await BinaryTrailing(
                    context,
                    tail,
                    separators,
                    complement
                );
                break;
            }
            if (tail is not (Cons or Nil))
            {
                throw StringSourceTrace.Clause(
                    context,
                    helper,
                    [tail, Term.I(0), await pattern.Pattern(context)],
                    complement ? StringSeparatorSource.TakeTrailingComplementHead : StringSeparatorSource.TakeTrailingHead
                );
            }
            bool scalar = tail is Cons { Head: Integer };
            bool ascii = complement && StringModernExecution.AsciiPair(tail, out _);
            int gcLine = complement ? StringSeparatorSource.TakeTrailingComplementGc
                : scalar ? StringSeparatorSource.TakeTrailingScalarGc : StringSeparatorSource.TakeTrailingGc;
            Cons? next = ascii ? (Cons)tail : await StringUnicode.Next(
                context,
                tail,
                true,
                new StringSourceStep(
                    helper,
                    3,
                    gcLine,
                    gcLine,
                    true
                )
            );
            if (next is null)
            {
                result = Term.Tuple(Nil.Value, Nil.Value);
                break;
            }
            bool accepted = separators.Contains(next.Head) != complement;
            pending.Add((next.Head, accepted, false));
            sourcePending.Push(complement
                ? ascii ? accepted ? StringSeparatorSource.TakeComplementAsciiYes : StringSeparatorSource.TakeComplementAsciiNo
                    : accepted ? StringSeparatorSource.TakeComplementNestedYes : StringSeparatorSource.TakeComplementNestedNo
                : scalar ? accepted ? StringSeparatorSource.TakeTrailingScalarYes : StringSeparatorSource.TakeTrailingScalarNo
                    : accepted ? StringSeparatorSource.TakeTrailingNestedYes : StringSeparatorSource.TakeTrailingNestedNo);
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
        string helper = complement ? StringSeparatorSource.TakeTrailingComplement : StringSeparatorSource.TakeTrailing;
        while (true)
        {
            var first = await StringSeparatorScan.ScanFor(
                context,
                binary.SliceBits(start * 8, binary.BitLength - start * 8),
                continuation,
                separators,
                !complement,
                helper,
                3,
                complement ? list ? StringSeparatorSource.TakeTrailingComplementListBinary : StringSeparatorSource.TakeTrailingComplementBinary
                    : list ? StringSeparatorSource.TakeTrailingListBinary : StringSeparatorSource.TakeTrailingBinary
            );
            if (!first.Found)
            {
                if (!list)
                {
                    return Term.Tuple(binary, new BitString([]));
                }
                Term used = await StringConsumedPrefix.Run(
                    context,
                    continuation,
                    first.Remainder,
                    helper,
                    complement ? StringSeparatorSource.TakeTrailingComplementConsumedPrefix : StringSeparatorSource.TakeTrailingConsumedPrefix
                );
                TupleTerm pair;
                using (StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    complement ? StringSeparatorSource.TakeComplementBinaryRecursion : StringSeparatorSource.TakeTrailingBinaryRecursion
                ))
                {
                    pair = (TupleTerm)await Trailing(
                        context,
                        first.Remainder,
                        separators,
                        complement
                    );
                }
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
            var inverse = await StringSeparatorScan.ScanFor(
                context,
                separatorStart,
                at.Tail,
                separators,
                complement,
                helper,
                3,
                complement ? list ? StringSeparatorSource.TakeTrailingComplementListSearch : StringSeparatorSource.TakeTrailingComplementSearch
                    : list ? StringSeparatorSource.TakeTrailingListInverse : StringSeparatorSource.TakeTrailingInverse
            );
            if (!inverse.Found)
            {
                TupleTerm pair;
                using (StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    complement ? StringSeparatorSource.TakeComplementInverseRecursion : StringSeparatorSource.TakeTrailingInverseRecursion
                ))
                {
                    pair = list ? (TupleTerm)await Trailing(
                        context,
                        inverse.Remainder,
                        separators,
                        complement
                    )
                        : Term.Tuple(Nil.Value, Nil.Value);
                }
                Term used = list ? await StringConsumedPrefix.Run(
                    context,
                    continuation,
                    inverse.Remainder,
                    helper,
                    complement ? StringSeparatorSource.TakeComplementInverseConsumedPrefix : StringSeparatorSource.TakeTrailingInverseConsumedPrefix
                ) : Nil.Value;
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

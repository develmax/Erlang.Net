namespace Erlang;

internal static class StringTrim
{
    internal static async ValueTask<Term> Leading(ProcessContext context, Term input, StringSeparatorSet separators)
    {
        while (true)
        {
            if (input is BitString { IsBinary: true } || input is Cons { Head: BitString { IsBinary: true } })
            {
                bool list = input is Cons;
                var scan = await StringSeparatorScan.ScanFor(
                    context,
                    list ? (BitString)((Cons)input).Head : (BitString)input,
                    list ? ((Cons)input).Tail : Nil.Value,
                    separators,
                    false,
                    StringSeparatorSource.TrimLeading,
                    2,
                    list ? StringSeparatorSource.TrimLeadingListBinary : StringSeparatorSource.TrimLeadingBinary
                );
                if (scan.Found)
                {
                    return list ? scan.Remainder : ((Cons)scan.Remainder).Head;
                }
                if (!list)
                {
                    return new BitString([]);
                }
                input = scan.Remainder;
                await context.ReduceAsync();
                continue;
            }
            if (input is not (Cons or Nil))
            {
                throw StringSourceTrace.Clause(
                    context,
                    StringSeparatorSource.TrimLeading,
                    [input, separators.Original],
                    StringSeparatorSource.TrimLeadingHead
                );
            }
            Cons? next = StringModernExecution.AsciiPair(input, out Cons direct) ? direct
                : await StringUnicode.Next(
                    context,
                    input,
                    true,
                    new StringSourceStep(
                        StringSeparatorSource.TrimLeading,
                        2,
                        StringSeparatorSource.TrimLeadingGc,
                        StringSeparatorSource.TrimLeadingGc,
                        true
                    )
                );
            if (next is null || !await separators.Member(
                context,
                next.Head,
                StringSeparatorSource.TrimLeading,
                2,
                StringModernExecution.AsciiPair(input, out _) ? StringSeparatorSource.TrimLeadingAsciiMember : StringSeparatorSource.TrimLeadingMember
            ))
            {
                return next is null ? Nil.Value : input;
            }
            input = next.Tail;
            await context.ReduceAsync();
        }
    }

    internal static async ValueTask<Term> Trailing(ProcessContext context, Term input, HashSet<Term> separators)
    {
        StringSeparatorSet pattern = StringSeparatorSet.From(separators);
        separators = pattern;
        bool single = pattern.Original is Cons { Head: Integer one, Tail: Nil } && one.Value >= 0 && one.Value <= 255 && input is Cons or Nil;
        using var singlePending = new StringSourceContinuations(context, StringSeparatorSource.TrimSingleTrailing, 2);
        using var sourcePending = new StringSourceContinuations(context, StringSeparatorSource.TrimTrailing, 3);
        var pending = new List<(Term Character, bool Separator, bool Raw)>();
        HashSet<Term> firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term result;
        while (true)
        {
            if (single)
            {
                if (input is Cons { Head: Integer } scalar)
                {
                    bool matches = separators.Contains(scalar.Head);
                    if (matches && scalar.Tail is Nil)
                    {
                        result = Nil.Value;
                        break;
                    }
                    if (!matches || StringModernExecution.AsciiPair(input, out _))
                    {
                        pending.Add((scalar.Head, matches, true));
                        singlePending.Push(matches ? StringSeparatorSource.TrimSingleEqualRecursion : StringSeparatorSource.TrimSingleRecursion);
                        input = scalar.Tail;
                        await context.ReduceAsync();
                        continue;
                    }
                }
                single = false;
            }
            if (input is Cons { Head: Integer } raw && !firstCodepoints.Contains(raw.Head))
            {
                pending.Add((raw.Head, false, true));
                sourcePending.Push(StringSeparatorSource.TrimTrailingRawRecursion);
                input = raw.Tail;
                await context.ReduceAsync();
                continue;
            }
            if (input is BitString { IsBinary: true } || input is Cons { Head: BitString { IsBinary: true } })
            {
                result = await BinaryTrailing(context, input, separators);
                break;
            }
            if (input is not (Cons or Nil))
            {
                throw StringSourceTrace.Clause(
                    context,
                    StringSeparatorSource.TrimTrailing,
                    [input, Term.I(0), await pattern.Pattern(context)],
                    StringSeparatorSource.TrimTrailingHead
                );
            }
            bool scalarInput = input is Cons { Head: Integer };
            int line = scalarInput ? StringSeparatorSource.TrimTrailingScalarGc : StringSeparatorSource.TrimTrailingGc;
            Cons? next = await StringUnicode.Next(
                context,
                input,
                true,
                new StringSourceStep(
                    StringSeparatorSource.TrimTrailing,
                    3,
                    line,
                    line,
                    true
                )
            );
            if (next is null)
            {
                result = Nil.Value;
                break;
            }
            pending.Add((next.Head, separators.Contains(next.Head), false));
            sourcePending.Push(scalarInput ? separators.Contains(next.Head) ? StringSeparatorSource.TrimTrailingScalarYes : StringSeparatorSource.TrimTrailingScalarNo : separators.Contains(next.Head) ? StringSeparatorSource.TrimTrailingNestedYes : StringSeparatorSource.TrimTrailingNestedNo);
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
            await StringSeparatorSet.From(separators).Compile(context, StringSeparatorSource.TrimTrailing, 3);
            var first = await StringSeparatorScan.ScanFor(
                context,
                binary.SliceBits(start * 8, binary.BitLength - start * 8),
                continuation,
                separators,
                true,
                StringSeparatorSource.TrimTrailing,
                3,
                list ? StringSeparatorSource.TrimTrailingListBinary : StringSeparatorSource.TrimTrailingBinary
            );
            if (!first.Found)
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    StringSeparatorSource.TrimTrailing,
                    3,
                    StringSeparatorSource.TrimTrailingBinaryRecursion
                );

                return list ? StringPatternSearch.Stack(binary, await Trailing(context, continuation, separators)) : binary;
            }
            var at = (Cons)first.Remainder;
            var separatorStart = (BitString)at.Head;
            var inverse = await StringSeparatorScan.ScanFor(
                context,
                separatorStart,
                at.Tail,
                separators,
                false,
                StringSeparatorSource.TrimTrailing,
                3,
                list ? StringSeparatorSource.TrimTrailingListInverse : StringSeparatorSource.TrimTrailingInverse
            );
            if (!inverse.Found)
            {
                Term tail;
                using (StringSourceTrace.Scope(
                    context,
                    StringSeparatorSource.TrimTrailing,
                    3,
                    StringSeparatorSource.TrimTrailingInverseRecursion
                ))
                {
                    tail = list ? await Trailing(context, inverse.Remainder, separators) : Nil.Value;
                }
                if (StringUnicode.IsEmpty(tail))
                {
                    return binary.SliceBits(0, binary.BitLength - separatorStart.BitLength);
                }
                Term used = await StringConsumedPrefix.Run(
                    context,
                    continuation,
                    inverse.Remainder,
                    StringSeparatorSource.TrimTrailing,
                    StringSeparatorSource.TrimConsumedPrefix
                );

                return StringPatternSearch.Stack(binary, StringPatternSearch.Stack(used, tail));
            }
            var nonSeparator = (Cons)inverse.Remainder;
            start = (binary.BitLength - ((BitString)nonSeparator.Head).BitLength) / 8;
            continuation = nonSeparator.Tail;
            await context.ReduceAsync();
        }
    }
}

using System.Numerics;

namespace Erlang;

internal static class StringLexemes
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term input,
        Term separatorList,
        BigInteger? number
    )
    {
        if (number == 1 && separatorList is Nil)
        {
            return input;
        }
        if (number is null && input is Nil)
        {
            return Nil.Value;
        }
        if (number is null && separatorList is Nil)
        {
            return Term.List(input);
        }
        if (separatorList is not (Cons or Nil))
        {
            throw StringLegacy.Clause();
        }
        var separators = await StringSeparatorSet.Create(
            context,
            separatorList,
            number is null ? StringFunctionNames.Lexemes : StringFunctionNames.NthLexeme,
            number is null ? 2 : 3,
            number is null ? StringSeparatorSource.LexemesPattern : StringSeparatorSource.NthPattern
        );
        var firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        string helper = number is null ? StringSeparatorSource.Lexemes : StringSeparatorSource.NthLexeme;
        var result = new List<Term>();
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString { IsBinary: true } };
            bool scalar = input is Cons { Head: Integer };
            int pickLine = scalar ? firstCodepoints.Contains(((Cons)input).Head) ? StringSeparatorSource.LexemesScalarPick : StringSeparatorSource.LexemesScalarOtherPick
                : listBinary ? StringSeparatorSource.LexemesListPick : input is BitString ? StringSeparatorSource.LexemesBinaryPick : StringSeparatorSource.LexemesNestedPick;
            if (input is BitString { IsBinary: true } && number is not null)
            {
                await separators.Compile(context, helper, 3);
            }
            if (input is BitString { IsBinary: true } || listBinary)
            {
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.ScanFor(
                    context,
                    binary,
                    continuation,
                    separators,
                    false,
                    helper,
                    3,
                    number is null ? listBinary ? StringSeparatorSource.LexemesListBinary : StringSeparatorSource.LexemesBinary
                        : listBinary ? StringSeparatorSource.NthListBinary : StringSeparatorSource.NthBinary
                );
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return number is null ? Cons.From(result) : new BitString([]);
                    }
                    input = scan.Remainder;
                    await context.ReduceAsync();
                    continue;
                }
                input = listBinary ? scan.Remainder : ((Cons)scan.Remainder).Head;
            }
            else
            {
                if (input is not (Cons or Nil))
                {
                    throw StringSourceTrace.Clause(
                        context,
                        helper,
                        [input, await separators.Pattern(context), number is null ? Cons.From(result.AsEnumerable().Reverse()) : new Integer(number.Value)],
                        number is null ? StringSeparatorSource.LexemesHead : StringSeparatorSource.NthHead
                    );
                }
                int gcLine = number is not null ? StringSeparatorSource.NthGc : scalar ? StringSeparatorSource.LexemesScalarGc : StringSeparatorSource.LexemesGc;
                Cons? next = number is null && scalar && !firstCodepoints.Contains(((Cons)input).Head) ? (Cons)input
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
                    return number is null ? Cons.From(result) : Nil.Value;
                }
                if (separators.Contains(next.Head))
                {
                    input = next.Tail;
                    await context.ReduceAsync();
                    continue;
                }
                if (number > 1)
                {
                    using (StringSourceTrace.Scope(
                        context,
                        helper,
                        3,
                        StringSeparatorSource.NthNestedSkip
                    ))
                    {
                        input = await Skip(context, next.Tail, separators);
                    }
                    number--;
                    await context.ReduceAsync();
                    continue;
                }
            }
            if (number > 1)
            {
                using (StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    listBinary ? StringSeparatorSource.NthListSkip : StringSeparatorSource.NthBinarySkip
                ))
                {
                    input = await Skip(context, input, separators);
                }
                number--;
                await context.ReduceAsync();
                continue;
            }
            await separators.Compile(context, helper, 3);
            TupleTerm picked;
            if (number is null)
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    helper,
                    3,
                    pickLine
                );
                picked = (TupleTerm)await Pick(context, input, separators);
            }
            else
            {
                picked = (TupleTerm)await Pick(context, input, separators);
            }
            if (number is not null)
            {
                return picked.Items[0];
            }
            if (!StringUnicode.IsEmpty(picked.Items[0]))
            {
                result.Add(picked.Items[0]);
            }
            input = picked.Items[1];
            await context.ReduceAsync();
        }
    }

    private static ValueTask<Term> EmptyPattern(ProcessContext context)
        => context.Runtime.Modules.Call(
            context,
            BinaryModuleNames.Module,
            BinaryModuleNames.CompilePattern,
            Nil.Value
        );

    private static async ValueTask<Term> Pick(ProcessContext context, Term input, StringSeparatorSet separators)
    {
        var firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term reverse = Nil.Value;
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString { IsBinary: true } };
            if (input is BitString { IsBinary: true } || listBinary)
            {
                if (separators.Count == 0)
                {
                    return await EmptyPattern(context);
                }
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.ScanFor(
                    context,
                    binary,
                    continuation,
                    separators,
                    true,
                    StringSeparatorSource.LexemePick,
                    3,
                    listBinary ? StringSeparatorSource.PickListBinary : StringSeparatorSource.PickBinary
                );
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return Term.Tuple(StringPatternSearch.BinaryToken(binary, reverse), Nil.Value);
                    }
                    reverse = new Cons(binary, reverse);
                    input = continuation;
                    await context.ReduceAsync();
                    continue;
                }
                var at = (Cons)scan.Remainder;
                var left = (BitString)at.Head;
                BitString keep = binary.SliceBits(0, binary.BitLength - left.BitLength);

                return Term.Tuple(StringPatternSearch.BinaryToken(keep, reverse), listBinary ? scan.Remainder : left);
            }
            if (input is not (Cons or Nil))
            {
                throw StringSourceTrace.Clause(
                    context,
                    StringSeparatorSource.LexemePick,
                    [input, await separators.Pattern(context), reverse],
                    StringSeparatorSource.PickHead
                );
            }
            bool scalar = input is Cons { Head: Integer };
            Cons? next = scalar ? (Cons)input : await StringUnicode.Next(
                context,
                input,
                false,
                new StringSourceStep(
                    StringSeparatorSource.LexemePick,
                    3,
                    StringSeparatorSource.PickCp,
                    StringSeparatorSource.PickCp,
                    true
                )
            );
            if (next is null)
            {
                return Term.Tuple(StringPatternSearch.Reverse(reverse), Nil.Value);
            }
            if (firstCodepoints.Contains(next.Head))
            {
                int line = scalar ? StringSeparatorSource.PickScalarGc : StringSeparatorSource.PickGc;
                Cons cluster = (await StringUnicode.Next(
                    context,
                    input,
                    true,
                    new StringSourceStep(
                        StringSeparatorSource.LexemePick,
                        3,
                        line,
                        line,
                        true
                    )
                ))!;
                if (separators.Contains(cluster.Head))
                {
                    return Term.Tuple(StringPatternSearch.Reverse(reverse), cluster.Tail);
                }
                foreach (Term character in cluster.Head is Integer ? [cluster.Head] : Cons.Items(cluster.Head))
                {
                    reverse = new Cons(character, reverse);
                }
                input = cluster.Tail;
            }
            else
            {
                reverse = new Cons(next.Head, reverse);
                input = next.Tail;
            }
            await context.ReduceAsync();
        }
    }

    private static async ValueTask<Term> Skip(ProcessContext context, Term input, StringSeparatorSet separators)
    {
        var firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString { IsBinary: true } };
            if (input is BitString { IsBinary: true } || listBinary)
            {
                await separators.Compile(context, StringSeparatorSource.LexemeSkip, 2);
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.ScanFor(
                    context,
                    binary,
                    continuation,
                    separators,
                    true,
                    StringSeparatorSource.LexemeSkip,
                    2,
                    listBinary ? StringSeparatorSource.SkipListBinary : StringSeparatorSource.SkipBinary
                );
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return new BitString([]);
                    }
                    input = continuation;
                    await context.ReduceAsync();
                    continue;
                }

                int line = listBinary ? StringSeparatorSource.SkipListBinaryGc : StringSeparatorSource.SkipBinaryGc;

                return (await StringUnicode.Next(
                    context,
                    listBinary ? scan.Remainder : ((Cons)scan.Remainder).Head,
                    true,
                    new StringSourceStep(
                        StringSeparatorSource.LexemeSkip,
                        2,
                        line,
                        line,
                        true
                    )
                ))!.Tail;
            }
            if (input is not (Cons or Nil))
            {
                throw StringSourceTrace.Clause(
                    context,
                    StringSeparatorSource.LexemeSkip,
                    [input, await separators.Pattern(context)],
                    StringSeparatorSource.SkipHead
                );
            }
            bool scalar = input is Cons { Head: Integer };
            Cons? next = scalar ? (Cons)input : await StringUnicode.Next(
                context,
                input,
                false,
                new StringSourceStep(
                    StringSeparatorSource.LexemeSkip,
                    2,
                    StringSeparatorSource.SkipCp,
                    StringSeparatorSource.SkipCp,
                    true
                )
            );
            if (next is null)
            {
                return Nil.Value;
            }
            if (firstCodepoints.Contains(next.Head))
            {
                int line = scalar ? StringSeparatorSource.SkipScalarGc : StringSeparatorSource.SkipGc;
                Cons cluster = (await StringUnicode.Next(
                    context,
                    input,
                    true,
                    new StringSourceStep(
                        StringSeparatorSource.LexemeSkip,
                        2,
                        line,
                        line,
                        true
                    )
                ))!;
                if (separators.Contains(cluster.Head))
                {
                    return cluster.Tail;
                }
                input = cluster.Tail;
            }
            else
            {
                input = next.Tail;
            }
            await context.ReduceAsync();
        }
    }
}

namespace Erlang;

internal static class StringLexemes
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term input, Term separatorList, int? number)
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
        var separators = Cons.Items(separatorList).ToHashSet();
        var result = new List<Term>();
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString };
            if (input is BitString && number is not null && separators.Count == 0)
            {
                return await EmptyPattern(context);
            }
            if (input is BitString || listBinary)
            {
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.Scan(context, binary, continuation, separators, false);
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return number is null ? Cons.From(result) : new BitString([]);
                    }
                    input = scan.Remainder;
                    continue;
                }
                input = listBinary ? scan.Remainder : ((Cons)scan.Remainder).Head;
            }
            else
            {
                if (input is not (Cons or Nil))
                {
                    throw StringLegacy.Clause();
                }
                Cons? next = await StringUnicode.Next(context, input, true);
                if (next is null)
                {
                    return number is null ? Cons.From(result) : Nil.Value;
                }
                if (separators.Contains(next.Head))
                {
                    input = next.Tail;
                    continue;
                }
                if (number > 1)
                {
                    input = await Skip(context, next.Tail, separators);
                    number--;
                    continue;
                }
            }
            if (number > 1)
            {
                input = await Skip(context, input, separators);
                number--;
                continue;
            }
            var picked = (TupleTerm)await Pick(context, input, separators);
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
        => context.Runtime.Modules.Call(context, BinaryModuleNames.Module, BinaryModuleNames.CompilePattern, Nil.Value);

    private static async ValueTask<Term> Pick(ProcessContext context, Term input, HashSet<Term> separators)
    {
        var firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        Term reverse = Nil.Value;
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString };
            if (input is BitString || listBinary)
            {
                if (separators.Count == 0)
                {
                    return await EmptyPattern(context);
                }
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.Scan(context, binary, continuation, separators, true);
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return Term.Tuple(StringPatternSearch.BinaryToken(binary, reverse), Nil.Value);
                    }
                    reverse = new Cons(binary, reverse);
                    input = continuation;
                    continue;
                }
                var at = (Cons)scan.Remainder;
                var left = (BitString)at.Head;
                BitString keep = binary.SliceBits(0, binary.BitLength - left.BitLength);

                return Term.Tuple(StringPatternSearch.BinaryToken(keep, reverse), listBinary ? scan.Remainder : left);
            }
            Cons? next = input is Cons { Head: Integer } raw ? raw : await StringUnicode.Next(context, input, false);
            if (next is null)
            {
                return Term.Tuple(StringPatternSearch.Reverse(reverse), Nil.Value);
            }
            if (firstCodepoints.Contains(next.Head))
            {
                Cons cluster = (await StringUnicode.Next(context, input, true))!;
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

    private static async ValueTask<Term> Skip(ProcessContext context, Term input, HashSet<Term> separators)
    {
        var firstCodepoints = await StringSeparatorScan.FirstCodepoints(context, separators);
        while (true)
        {
            bool listBinary = input is Cons { Head: BitString };
            if (input is BitString || listBinary)
            {
                if (separators.Count == 0)
                {
                    return await EmptyPattern(context);
                }
                BitString binary = listBinary ? (BitString)((Cons)input).Head : (BitString)input;
                Term continuation = listBinary ? ((Cons)input).Tail : Nil.Value;
                var scan = await StringSeparatorScan.Scan(context, binary, continuation, separators, true);
                if (!scan.Found)
                {
                    if (!listBinary)
                    {
                        return new BitString([]);
                    }
                    input = continuation;
                    continue;
                }

                return (await StringUnicode.Next(context, listBinary ? scan.Remainder : ((Cons)scan.Remainder).Head, true))!.Tail;
            }
            Cons? next = input is Cons { Head: Integer } raw ? raw : await StringUnicode.Next(context, input, false);
            if (next is null)
            {
                return Nil.Value;
            }
            if (firstCodepoints.Contains(next.Head))
            {
                Cons cluster = (await StringUnicode.Next(context, input, true))!;
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

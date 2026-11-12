namespace Erlang;

internal static class StringSeparatorScan
{
    internal static async ValueTask<HashSet<Term>> FirstCodepoints(ProcessContext context, HashSet<Term> separators)
    {
        if (separators is StringSeparatorSet actual)
        {
            return Cons.Items(await actual.Codepoints(context)).ToHashSet();
        }
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
        StringSeparatorSet pattern = StringSeparatorSet.From(separators);
        if (!member)
        {
            return await StringInverseBinaryScan.Run(
                context,
                binary,
                continuation,
                pattern
            );
        }
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        await pattern.Compile(context, caller?.Function ?? StringSeparatorSource.SearchLoop, caller?.Arity ?? 5);
        BitString rest = binary;
        while (rest.BitLength > 0)
        {
            Term matched;
            using (StringSourceTrace.Scope(
                context,
                StringSeparatorSource.SearchLoop,
                5,
                StringSeparatorSource.SearchMatch
            ))
            {
                matched = await context.Runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Match,
                    rest,
                    pattern.Compiled!
                );
            }
            if (matched is not TupleTerm pair)
                return (false, continuation);
            int position = (int)((Integer)pair.Items[0]).Value;
            rest = rest.SliceBits(position * UnicodeNativeNames.BitsPerByte, rest.BitLength - position * UnicodeNativeNames.BitsPerByte);
            Term at = continuation is Nil ? rest : new Cons(rest, continuation);
            int line = continuation is Nil ? StringSeparatorSource.SearchGc : StringSeparatorSource.SearchListGc;
            Term value;
            using (StringSourceTrace.Scope(
                context,
                StringSeparatorSource.SearchLoop,
                5,
                line
            ))
            {
                value = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, at);
            }
            if (value is not Cons next)
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value),
                    StringSeparatorSource.SearchLoop,
                    5,
                    line
                );
            if (await pattern.Member(
                context,
                next.Head,
                StringSeparatorSource.SearchLoop,
                5,
                continuation is Nil ? StringSeparatorSource.SearchMember : StringSeparatorSource.SearchListMember
            ))
                return (true, new Cons(rest, continuation));
            if (next.Tail is BitString remainder && continuation is Nil)
                rest = remainder;
            else if (next.Tail is Cons { Head: BitString remaining } cell && cell.Tail.Equals(continuation))
                rest = remaining;
            else
                return (false, next.Tail);
            await context.ReduceAsync();
        }

        return (false, continuation);
    }

    internal static async ValueTask<(bool Found, Term Remainder)> ScanFor(
        ProcessContext context,
        BitString binary,
        Term continuation,
        HashSet<Term> separators,
        bool member,
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

        return await Scan(
            context,
            binary,
            continuation,
            separators,
            member
        );
    }

}

namespace Erlang;

internal static class StringConsumedPrefix
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term original,
        Term continuation,
        string caller,
        int callLine
    )
    {
        using var owner = StringSourceTrace.Scope(
            context,
            caller,
            3,
            callLine
        );
        Term point;
        using (StringSourceTrace.Scope(
            context,
            StringSeparatorSource.ConsumedPrefix,
            2,
            StringSeparatorSource.ConsumedPrefixCp
        ))
        {
            point = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, continuation);
        }
        if (point is Nil)
            return original;
        if (point is not Cons stop)
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), point),
                StringSeparatorSource.ConsumedPrefix,
                2,
                StringSeparatorSource.ConsumedPrefixCp
            );
        var values = new List<Term>();
        using var pending = new StringSourceContinuations(context, StringSeparatorSource.ConsumedPrefixLoop, 3);
        while (true)
        {
            Term next;
            using (StringSourceTrace.Scope(
                context,
                StringSeparatorSource.ConsumedPrefixLoop,
                3,
                StringSeparatorSource.ConsumedPrefixLoopCp
            ))
            {
                next = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, original);
            }
            if (next is not Cons current)
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), next),
                    StringSeparatorSource.ConsumedPrefixLoop,
                    3,
                    StringSeparatorSource.ConsumedPrefixLoopCp
                );
            int line = StringSeparatorSource.ConsumedPrefixOther;
            if (current.Head.Equals(stop.Head))
            {
                Term equal;
                using (StringSourceTrace.Scope(
                    context,
                    StringSeparatorSource.ConsumedPrefixLoop,
                    3,
                    StringSeparatorSource.ConsumedPrefixEqual
                ))
                {
                    equal = await StringComparisonExecution.Equal(context, [current.Tail, stop.Tail]);
                }
                if (equal.Equals(Term.A(CommonAtomNames.True)))
                    return Cons.From(values);
                line = StringSeparatorSource.ConsumedPrefixSame;
            }
            values.Add(current.Head);
            original = current.Tail;
            pending.Push(line);
            await context.ReduceAsync();
        }
    }
}

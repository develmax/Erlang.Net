namespace Erlang;

internal static class StringInverseBinaryScan
{
    internal static async ValueTask<(bool Found, Term Remainder)> Run(
        ProcessContext context,
        BitString binary,
        Term continuation,
        StringSeparatorSet separators
    )
    {
        bool single = separators.Original is Cons { Tail: Nil };
        string helper = single ? StringSeparatorSource.InverseSingle : StringSeparatorSource.InverseMultiple;
        BitString rest = binary;
        while (rest.BitLength > 0)
        {
            if (!StringModernExecution.Decode(rest, out Integer first, out BitString afterFirst))
            {
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), rest),
                    helper,
                    3,
                    single ? StringSeparatorSource.SingleError : StringSeparatorSource.MultipleError
                );
            }
            if (StringModernExecution.Decode(afterFirst, out Integer second, out _)
                && first.Value < 256 && second.Value < 256 && first.Value != '\r')
            {
                bool accepted = single ? first.Equals(((Cons)separators.Original).Head)
                    : await separators.Member(
                        context,
                        first,
                        helper,
                        3,
                        StringSeparatorSource.MultipleAsciiMember
                    );
                if (!accepted)
                    return (true, new Cons(rest, continuation));
                rest = afterFirst;
                await context.ReduceAsync();
                continue;
            }
            int line = single ? continuation is Nil ? StringSeparatorSource.SingleGc : StringSeparatorSource.SingleListGc
                : continuation is Nil ? StringSeparatorSource.MultipleGc : StringSeparatorSource.MultipleListGc;
            Term value;
            using (StringSourceTrace.Scope(
                context,
                helper,
                3,
                line
            ))
            {
                value = await StringUnicode.Utility(
                    context,
                    StringUnicodeDependencyNames.Grapheme,
                    continuation is Nil ? rest : new Cons(rest, continuation)
                );
            }
            if (value is not Cons next)
            {
                if (single)
                    return (true, new Cons(rest, continuation));
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value),
                    helper,
                    3,
                    line
                );
            }
            bool member = single ? next.Head.Equals(((Cons)separators.Original).Head)
                : await separators.Member(
                    context,
                    next.Head,
                    helper,
                    3,
                    continuation is Nil ? StringSeparatorSource.MultipleMember : StringSeparatorSource.MultipleListMember
                );
            if (!member)
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
}

namespace Erlang;

internal static class StringLegacySearchExecution
{
    internal static async ValueTask<Term> Invoke(
        ProcessContext context,
        string function,
        Term input,
        Term argument
    )
    {
        if (function is StringFunctionNames.Chr or StringFunctionNames.Rchr)
        {
            bool reverse = function == StringFunctionNames.Rchr;
            if (argument is not Integer)
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    [input, argument],
                    reverse ? StringLegacySource.Rchr : StringLegacySource.Chr
                );
            }
            int position = 1;
            int found = 0;
            while (input is Cons cell)
            {
                if (cell.Head.Equals(argument))
                {
                    found = position;
                    if (!reverse)
                    {
                        return Term.I(found);
                    }
                }
                input = cell.Tail;
                position++;
                await context.ReduceAsync();
            }
            if (input is not Nil)
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    reverse ? [input, argument, Term.I(position), Term.I(found)] : [input, argument, Term.I(position)],
                    reverse ? StringLegacySource.RchrLoop : StringLegacySource.ChrLoop
                );
            }

            return Term.I(found);
        }
        if (function == StringFunctionNames.Tokens)
        {
            return await Tokens(context, input, argument);
        }
        bool span = function is StringFunctionNames.Span or StringFunctionNames.Cspan;
        int entryLine = function switch
        {
            StringFunctionNames.Str => StringLegacySource.Str,
            StringFunctionNames.Rstr => StringLegacySource.Rstr,
            StringFunctionNames.Span => StringLegacySource.Span,
            _ => StringLegacySource.Cspan
        };
        if (argument is not (Cons or Nil))
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                [input, argument],
                entryLine
            );
        }
        if (span)
        {
            int count = 0;
            int line = function == StringFunctionNames.Span ? StringLegacySource.SpanLoop : StringLegacySource.CspanLoop;
            while (input is Cons cell)
            {
                Term member;
                using (StringSourceTrace.Scope(
                    context,
                    function,
                    3,
                    line + 1
                ))
                {
                    member = await context.Runtime.Modules.Call(
                        context,
                        ListModuleNames.Module,
                        ListModuleNames.Member,
                        cell.Head,
                        argument
                    );
                }
                if (member.Equals(Term.A(CommonAtomNames.True)) != (function == StringFunctionNames.Span))
                {
                    return Term.I(count);
                }
                count++;
                input = cell.Tail;
            }
            if (input is not Nil)
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    [input, argument, Term.I(count)],
                    line
                );
            }

            return Term.I(count);
        }
        int index = 1;
        int last = 0;
        bool trailing = function == StringFunctionNames.Rstr;
        while (input is Cons cell)
        {
            if (argument is Cons needle && needle.Head.Equals(cell.Head))
            {
                bool matched;
                using (StringSourceTrace.Scope(
                    context,
                    function,
                    trailing ? 4 : 3,
                    trailing ? StringLegacySource.RstrPrefixCall : StringLegacySource.StrPrefixCall
                ))
                {
                    matched = Prefix(context, needle.Tail, cell.Tail);
                }
                if (matched)
                {
                    last = index;
                    if (!trailing)
                    {
                        return Term.I(index);
                    }
                }
            }
            input = cell.Tail;
            index++;
            await context.ReduceAsync();
        }
        if (input is not Nil)
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                trailing ? [input, argument, Term.I(index), Term.I(last)] : [input, argument, Term.I(index)],
                trailing ? StringLegacySource.RstrLoop : StringLegacySource.StrLoop
            );
        }

        return Term.I(last);
    }

    private static bool Prefix(ProcessContext context, Term prefix, Term input)
    {
        while (prefix is Cons p && input is Cons value && p.Head.Equals(value.Head))
        {
            prefix = p.Tail;
            input = value.Tail;
        }
        if (prefix is Nil && input is Cons or Nil)
        {
            return true;
        }
        if (prefix is Cons or Nil && input is Cons or Nil)
        {
            return false;
        }

        throw StringSourceTrace.Clause(
            context,
            StringSourceNames.ListPrefix,
            [prefix, input],
            StringLegacySource.Prefix
        );
    }

    private static async ValueTask<Term> Tokens(ProcessContext context, Term input, Term separators)
    {
        if (separators is Nil)
        {
            if (input is Nil)
            {
                return Nil.Value;
            }
            if (input is Cons)
            {
                return Term.List(input);
            }

            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), input),
                StringFunctionNames.Tokens,
                2,
                StringLegacySource.TokensEmptyCase
            );
        }
        if (separators is not Cons)
        {
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), separators),
                StringFunctionNames.Tokens,
                2,
                StringLegacySource.Tokens
            );
        }
        bool single = ((Cons)separators).Tail is Nil;
        Term reversed;
        using (StringSourceTrace.Scope(
            context,
            StringFunctionNames.Tokens,
            2,
            single ? StringLegacySource.TokensSingleReverse : StringLegacySource.TokensMultipleReverse
        ))
        {
            reversed = await context.Runtime.Modules.Call(
                context,
                ListModuleNames.Module,
                ListModuleNames.Reverse,
                input
            );
        }
        Term tokens = Nil.Value;
        Term token = Nil.Value;
        while (reversed is Cons cell)
        {
            bool separator;
            if (single)
            {
                separator = ((Cons)separators).Head.Equals(cell.Head);
            }
            else
            {
                using var call = StringSourceTrace.Scope(
                    context,
                    token is Nil ? StringSourceNames.TokensMultipleStart : StringSourceNames.TokensMultiplePick,
                    token is Nil ? 3 : 4,
                    token is Nil ? StringLegacySource.TokensMultipleStart : StringLegacySource.TokensMultiplePick
                );
                separator = (await context.Runtime.Modules.Call(
                    context,
                    ListModuleNames.Module,
                    ListModuleNames.Member,
                    cell.Head,
                    separators
                ))
                    .Equals(Term.A(CommonAtomNames.True));
            }
            if (separator)
            {
                if (token is not Nil)
                {
                    tokens = new Cons(token, tokens);
                    token = Nil.Value;
                }
            }
            else
            {
                token = new Cons(cell.Head, token);
            }
            reversed = cell.Tail;
            await context.ReduceAsync();
        }

        return token is Nil ? tokens : new Cons(token, tokens);
    }
}

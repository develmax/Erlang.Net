namespace Erlang;

internal static class StringLegacySearch
{
    internal static Term Invoke(string function, Term input, Term argument)
    {
        if (function is StringFunctionNames.Chr or StringFunctionNames.Rchr)
        {
            _ = StringLegacy.IntegerValue(argument);
            int position = 1;
            int found = 0;
            while (input is Cons cell)
            {
                if (cell.Head.Equals(argument))
                {
                    found = position;
                    if (function == StringFunctionNames.Chr)
                    {
                        return Term.I(found);
                    }
                }
                input = cell.Tail;
                position++;
            }
            if (input is not Nil)
            {
                throw StringLegacy.Clause();
            }

            return Term.I(found);
        }
        if (function == StringFunctionNames.Tokens)
        {
            if (argument is Nil)
            {
                return input switch
                {
                    Nil => Nil.Value,
                    Cons => Term.List(input),
                    _ => throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), input))
                };
            }
            if (argument is not Cons)
            {
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), argument));
            }
            var items = new List<Term>();
            while (input is Cons cell)
            {
                items.Add(cell.Head);
                input = cell.Tail;
            }
            if (input is not Nil)
            {
                throw StringLegacy.Clause();
            }
            Term[] values = items.ToArray();
            var tokens = new List<Term>();
            int start = 0;
            for (int index = 0; index <= values.Length; index++)
            {
                if (index == values.Length || Cons.Items(argument).Any(value => value.Equals(values[index])))
                {
                    if (index > start)
                    {
                        tokens.Add(Cons.From(values.Skip(start).Take(index - start)));
                    }
                    start = index + 1;
                }
            }

            return Cons.From(tokens);
        }
        if (argument is not (Cons or Nil))
        {
            throw StringLegacy.Clause();
        }
        if (function is StringFunctionNames.Span or StringFunctionNames.Cspan)
        {
            int count = 0;
            while (input is Cons cell)
            {
                bool member = Cons.Items(argument).Any(value => value.Equals(cell.Head));
                if (member != (function == StringFunctionNames.Span))
                {
                    return Term.I(count);
                }
                count++;
                input = cell.Tail;
            }
            if (input is not Nil)
            {
                throw StringLegacy.Clause();
            }

            return Term.I(count);
        }
        int current = 1;
        int last = 0;
        while (input is Cons cell)
        {
            if (argument is Cons needle && needle.Head.Equals(cell.Head) && Prefix(needle.Tail, cell.Tail))
            {
                last = current;
                if (function == StringFunctionNames.Str)
                {
                    return Term.I(last);
                }
            }
            input = cell.Tail;
            current++;
        }
        if (input is not Nil)
        {
            throw StringLegacy.Clause();
        }

        return Term.I(last);
    }

    private static bool Prefix(Term prefix, Term input)
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

        throw StringLegacy.Clause();
    }
}

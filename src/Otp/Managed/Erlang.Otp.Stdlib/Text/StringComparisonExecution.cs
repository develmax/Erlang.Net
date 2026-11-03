namespace Erlang;

internal static class StringComparisonExecution
{
    internal static async ValueTask<Term> Equal(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        bool fold = arguments.Count > 2 && arguments[2].Equals(Term.A(CommonAtomNames.True));
        Term normalization = arguments.Count == 4 ? arguments[3] : Term.A(StringTextAtoms.NoNormalization);
        bool normalized = !normalization.Equals(Term.A(StringTextAtoms.NoNormalization));
        if (arguments.Count > 2 && !fold && !arguments[2].Equals(Term.A(CommonAtomNames.False)))
        {
            bool three = arguments.Count == 3 || !normalized;
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Equal,
                three ? arguments.Take(3).ToArray() : arguments,
                three ? StringComparisonSource.EqualThreeHead : StringComparisonSource.EqualFourHead
            );
        }
        Term left = arguments[0];
        Term right = arguments[1];
        string helper = normalized ? fold ? StringComparisonSource.EqualNormalizedFold : StringComparisonSource.EqualNormalized
            : fold ? StringComparisonSource.EqualFold : StringComparisonSource.Equal;
        int line = normalized ? fold ? StringComparisonSource.EqualNormalizedFoldCall : StringComparisonSource.EqualNormalizedCall
            : fold ? StringComparisonSource.EqualFoldCall : StringComparisonSource.EqualCall;
        int arity = normalized ? 3 : 2;
        if (!fold && !normalized && left is BitString { IsBinary: true } && right is BitString { IsBinary: true })
        {
            return Term.A(left.Equals(right) ? CommonAtomNames.True : CommonAtomNames.False);
        }
        while (true)
        {
            if ((fold || normalized) && left.Equals(right))
            {
                return Term.A(CommonAtomNames.True);
            }
            if (!fold && !normalized)
            {
                if (left is Cons { Head: Integer a } lc && right is Cons { Head: Integer b } rc)
                {
                    if (!a.Equals(b))
                    {
                        return Term.A(CommonAtomNames.False);
                    }
                    left = lc.Tail;
                    right = rc.Tail;
                    await context.ReduceAsync();
                    continue;
                }
                if (left is Nil)
                {
                    return Term.A(StringUnicode.IsEmpty(right) ? CommonAtomNames.True : CommonAtomNames.False);
                }
            }
            Term l;
            Term r;
            using (StringSourceTrace.Scope(
                context,
                helper,
                arity,
                line
            ))
            {
                l = await Point(
                    context,
                    left,
                    fold,
                    normalized ? normalization : null
                );
            }
            using (StringSourceTrace.Scope(
                context,
                helper,
                arity,
                fold || normalized ? line + 1 : line
            ))
            {
                r = await Point(
                    context,
                    right,
                    fold,
                    normalized ? normalization : null
                );
            }
            if (l is Cons lcp && r is Cons rcp && lcp.Head.Equals(rcp.Head))
            {
                left = lcp.Tail;
                right = rcp.Tail;
                await context.ReduceAsync();
                continue;
            }
            if (l is Cons or Nil && r is Cons or Nil)
            {
                return Term.A(l is Nil && r is Nil ? CommonAtomNames.True : CommonAtomNames.False);
            }
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), Term.Tuple(l, r)),
                helper,
                arity,
                line
            );
        }
    }

    private static async ValueTask<Term> Point(
        ProcessContext context,
        Term input,
        bool fold,
        Term? normalization
    )
    {
        if (normalization is not null)
        {
            if (normalization is not Atom name)
            {
                return await context.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangBasicBifNames.Apply,
                    Term.A(StringUnicodeDependencyNames.Module),
                    normalization,
                    Term.List(input)
                );
            }
            input = await StringUnicode.Utility(context, name.Name, input);
        }
        if (fold)
        {
            input = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Casefold, input);
        }

        return await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, input);
    }

    internal static async ValueTask<List<Term>> SimilarityElements(ProcessContext context, Term input, bool indexMap)
    {
        var values = new List<Term>();
        string start = indexMap ? StringComparisonSource.IndexMap : StringComparisonSource.GraphemeLength;
        string loop = indexMap ? StringComparisonSource.MapLoop : StringComparisonSource.GraphemeLoop;
        using var caller = StringSourceTrace.Scope(
            context,
            StringFunctionNames.JaroSimilarity,
            2,
            indexMap ? StringComparisonSource.SimilarityRight : StringComparisonSource.SimilarityLeft
        );
        Term result;
        using (StringSourceTrace.Scope(
            context,
            start,
            1,
            indexMap ? StringComparisonSource.IndexStart : StringComparisonSource.GraphemeStart
        ))
        {
            result = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, input);
        }
        using var indexCaller = indexMap ? StringSourceTrace.Scope(
            context,
            start,
            1,
            StringComparisonSource.IndexStart
        ) : null;
        using var pending = new StringSourceContinuations(context, loop, indexMap ? 2 : 3);
        while (result is Cons cell)
        {
            values.Add(cell.Head);
            int line = indexMap ? StringComparisonSource.IndexNext : StringComparisonSource.GraphemeNext;
            using (StringSourceTrace.Scope(
                context,
                loop,
                indexMap ? 2 : 3,
                line
            ))
            {
                result = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Grapheme, cell.Tail);
            }
            if (indexMap)
            {
                pending.Push(line);
            }
            await context.ReduceAsync();
        }
        if (result is not Nil)
        {
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), ((TupleTerm)result).Items[1]),
                loop,
                indexMap ? 2 : 3,
                indexMap ? StringComparisonSource.IndexError : StringComparisonSource.GraphemeError
            );
        }

        return values;
    }
}

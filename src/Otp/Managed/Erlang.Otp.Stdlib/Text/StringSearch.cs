namespace Erlang;

internal static class StringSearch
{
    internal static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> a)
    {
        Term input = a[0];
        switch (function)
        {
            case StringFunctionNames.Prefix:
                {
                    StringSearchPattern needle;
                    using (StringSourceTrace.Scope(
                        context,
                        StringFunctionNames.Prefix,
                        2,
                        StringSearchSource.PrefixConvert
                    ))
                    {
                        needle = new StringSearchPattern(await context.Runtime.Modules.Call(
                            context,
                            UnicodeNativeNames.Module,
                            UnicodeNativeNames.ToList,
                            a[1]
                        ));
                    }

                    if (needle.Original is Nil)
                    {
                        return input;
                    }
                    using var call = StringSourceTrace.Scope(
                        context,
                        StringFunctionNames.Prefix,
                        2,
                        StringSearchSource.PrefixPublic
                    );
                    Term result = await StringPatternSearch.Prefix(context, input, needle) ?? Term.A(StringTextAtoms.NoMatch);

                    return input is BitString && result is Nil ? StringUnicode.Empty(input) : result;
                }
            case StringFunctionNames.Trim:
            case StringFunctionNames.Chomp:
                {
                    Term separators = function == StringFunctionNames.Chomp ? Term.List(Term.String("\r\n"), Term.I('\n'))
                        : a.Count == 3 ? a[2] : await Whitespace(context);
                    Term directionTerm = a.Count > 1 ? a[1] : Term.A(StringTextAtoms.Both);
                    string direction = directionTerm is Atom atom ? atom.Name : "";
                    if (function == StringFunctionNames.Chomp)
                    {
                        separators = Term.List(Term.String("\r\n"), Term.I('\n'));
                        direction = StringTextAtoms.Trailing;
                    }
                    if (separators is Nil)
                    {
                        return input;
                    }
                    if (direction is not (StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.Both) || separators is not (Cons or Nil))
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            StringFunctionNames.Trim,
                            [input, directionTerm, separators],
                            StringSeparatorSource.TrimHead
                        );
                    }
                    var set = new StringSeparatorSet(separators);
                    if (direction is StringTextAtoms.Leading or StringTextAtoms.Both)
                    {
                        if (direction == StringTextAtoms.Both)
                        {
                            using var call = StringSourceTrace.Scope(
                                context,
                                StringFunctionNames.Trim,
                                3,
                                StringSeparatorSource.TrimBoth
                            );
                            input = await StringTrim.Leading(context, input, set);
                        }
                        else
                        {
                            input = await StringTrim.Leading(context, input, set);
                        }
                    }
                    if (direction is StringTextAtoms.Trailing or StringTextAtoms.Both)
                    {
                        bool single = input is Cons or Nil && separators is Cons { Head: Integer cp, Tail: Nil } && cp.Value >= 0 && cp.Value < 256;
                        if (!single)
                        {
                            set = await StringSeparatorSet.Create(
                                context,
                                separators,
                                StringFunctionNames.Trim,
                                3,
                                StringSearchSource.TrimPattern
                            );
                        }
                        input = await StringTrim.Trailing(context, input, set);
                    }

                    return input;
                }
            case StringFunctionNames.Take:
                {
                    Term complementTerm = a.Count > 2 ? a[2] : Term.A(CommonAtomNames.False);
                    Term directionTerm = a.Count == 4 ? a[3] : Term.A(StringTextAtoms.Leading);
                    bool complement = complementTerm.Equals(Term.A(CommonAtomNames.True));
                    if (a[1] is Nil && ((!complement && !complementTerm.Equals(Term.A(CommonAtomNames.False)))
                        || directionTerm is not Atom { Name: StringTextAtoms.Leading or StringTextAtoms.Trailing }))
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), Term.Tuple(complementTerm, directionTerm)),
                            function,
                            4,
                            StringSearchSource.TakeEmptyCase
                        );
                    }
                    if (a.Count > 2 && !complement && !a[2].Equals(Term.A(CommonAtomNames.False)))
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            [input, a[1], complementTerm, directionTerm],
                            StringSearchSource.TakeHead
                        );
                    }
                    string direction = directionTerm is Atom atom ? atom.Name : "";
                    if (direction is not (StringTextAtoms.Leading or StringTextAtoms.Trailing))
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            [input, a[1], complementTerm, directionTerm],
                            StringSearchSource.TakeHead
                        );
                    }
                    StringSeparatorSet set = complement || direction == StringTextAtoms.Trailing
                        ? await StringSeparatorSet.Create(
                            context,
                            a[1],
                            function,
                            4,
                            direction == StringTextAtoms.Trailing ? complement ? StringSearchSource.TakeTrailingComplementPattern : StringSearchSource.TakeTrailingPattern : StringSearchSource.TakeLeadingPattern
                        )
                        : new StringSeparatorSet(a[1]);

                    return await Take(
                        context,
                        input,
                        set,
                        complement,
                        direction == StringTextAtoms.Trailing
                    );
                }
            case StringFunctionNames.Lexemes:
            case StringFunctionNames.NthLexeme:
                {
                    Term separators = a[^1];
                    if (function == StringFunctionNames.Lexemes && input is Nil)
                    {
                        return Nil.Value;
                    }
                    if (separators is not (Cons or Nil) || function == StringFunctionNames.NthLexeme && a[1] is not Integer { Value.Sign: > 0 })
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            a,
                            function == StringFunctionNames.NthLexeme ? StringSearchSource.NthLexemeHead : StringSearchSource.LexemesHead
                        );
                    }
                    Integer number = function == StringFunctionNames.NthLexeme ? (Integer)a[1] : Term.I(0);

                    return await StringLexemes.Run(
                        context,
                        input,
                        separators,
                        function == StringFunctionNames.NthLexeme ? number.Value : null
                    );
                }
            case StringFunctionNames.Find:
            case StringFunctionNames.Split:
            case StringFunctionNames.Replace:
                {
                    bool replace = function == StringFunctionNames.Replace;
                    int whereIndex = replace ? 3 : 2;
                    Term whereTerm = a.Count > whereIndex ? a[whereIndex] : Term.A(StringTextAtoms.Leading);
                    string where = whereTerm is Atom atom ? atom.Name : "";
                    if (replace)
                    {
                        using var replaceCall = StringSourceTrace.Scope(
                            context,
                            StringFunctionNames.Replace,
                            a.Count,
                            a.Count == 3 ? StringSearchSource.ReplaceThree : StringSearchSource.ReplaceFour
                        );
                        Term divided = await Invoke(context, StringFunctionNames.Split, [input, a[1], whereTerm]);
                        var joined = new List<Term>();
                        foreach (Term piece in Cons.Items(divided))
                        {
                            if (joined.Count > 0)
                            {
                                joined.Add(a[2]);
                            }
                            joined.Add(piece);
                        }

                        return Cons.From(joined);
                    }
                    if (function == StringFunctionNames.Find
                        ? a[1] is Nil or BitString { BitLength: 0 }
                        : StringUnicode.IsEmpty(a[1]))
                    {
                        return function == StringFunctionNames.Find ? input : Term.List(input);
                    }
                    if (function == StringFunctionNames.Find && where is not (StringTextAtoms.Leading or StringTextAtoms.Trailing))
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            [input, a[1], whereTerm],
                            StringSearchSource.FindHead
                        );
                    }
                    StringSearchPattern needle;
                    using (StringSourceTrace.Scope(
                        context,
                        function,
                        3,
                        function == StringFunctionNames.Split ? StringSearchSource.SplitConvert : where == StringTextAtoms.Trailing ? StringSearchSource.FindTrailingConvert : StringSearchSource.FindLeadingConvert
                    ))
                    {
                        needle = new StringSearchPattern(await context.Runtime.Modules.Call(
                            context,
                            UnicodeNativeNames.Module,
                            UnicodeNativeNames.ToList,
                            a[1]
                        ));
                    }
                    if (function == StringFunctionNames.Find)
                    {
                        return await StringPatternSearch.Find(
                            context,
                            input,
                            needle,
                            where == StringTextAtoms.Trailing
                        );
                    }
                    using var splitCall = StringSourceTrace.Scope(
                        context,
                        StringFunctionNames.Split,
                        3,
                        StringSearchSource.SplitPublic
                    );
                    Term split = await StringPatternSearch.Split(
                        context,
                        input,
                        needle,
                        whereTerm
                    );
                    var pieces = Cons.Items(split).ToList();

                    return Cons.From(pieces);
                }
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static ValueTask<Term> Whitespace(ProcessContext context)
        => context.Runtime.Modules.Call(context, StringUnicodeDependencyNames.Module, StringUnicodeDependencyNames.PatternWhitespace);

    private static async ValueTask<Term> Take(
        ProcessContext context,
        Term input,
        HashSet<Term> separators,
        bool complement,
        bool trailing
    )
    {
        if (separators is StringSeparatorSet { Original: Nil })
        {
            bool wholeFirst = complement != trailing;

            return wholeFirst ? Term.Tuple(input, StringUnicode.Empty(input)) : Term.Tuple(StringUnicode.Empty(input), input);
        }

        return trailing
            ? await StringTake.Trailing(
                context,
                input,
                separators,
                complement
            )
            : await StringTake.Leading(
                context,
                input,
                separators,
                complement
            );
    }
}

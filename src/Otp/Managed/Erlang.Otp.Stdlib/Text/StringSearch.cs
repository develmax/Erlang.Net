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
                    List<Term> needle = await StringUnicode.Elements(context, a[1], false);

                    return await Prefix(context, input, needle) ?? Term.A(StringTextAtoms.NoMatch);
                }
            case StringFunctionNames.Trim:
            case StringFunctionNames.Chomp:
                {
                    Term separators = function == StringFunctionNames.Chomp ? Term.List(Term.I('\n'), Term.String("\r\n"))
                        : a.Count == 3 ? a[2] : await Whitespace(context);
                    string direction = a.Count > 1 && a[1] is Atom atom ? atom.Name : a.Count == 1 ? StringTextAtoms.Both : throw StringLegacy.Clause();
                    if (function == StringFunctionNames.Chomp)
                    {
                        separators = Term.List(Term.I('\n'), Term.String("\r\n"));
                        direction = StringTextAtoms.Trailing;
                    }
                    if (separators is Nil)
                    {
                        return input;
                    }
                    var set = Cons.Items(separators).ToHashSet();
                    if (direction is not (StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.Both))
                    {
                        throw StringLegacy.Clause();
                    }
                    if (direction is StringTextAtoms.Leading or StringTextAtoms.Both)
                    {
                        input = ((TupleTerm)await Take(
                            context,
                            input,
                            set,
                            false,
                            false
                        )).Items[1];
                    }
                    if (direction is StringTextAtoms.Trailing or StringTextAtoms.Both)
                    {
                        input = await StringTrim.Trailing(context, input, set);
                    }

                    return input;
                }
            case StringFunctionNames.Take:
                {
                    bool complement = a.Count > 2 && a[2].Equals(Term.A(CommonAtomNames.True));
                    if (a.Count > 2 && !complement && !a[2].Equals(Term.A(CommonAtomNames.False)))
                    {
                        throw StringLegacy.Clause();
                    }
                    string direction = a.Count == 4 && a[3] is Atom atom ? atom.Name : a.Count < 4 ? StringTextAtoms.Leading : throw StringLegacy.Clause();
                    if (direction is not (StringTextAtoms.Leading or StringTextAtoms.Trailing))
                    {
                        throw StringLegacy.Clause();
                    }

                    return await Take(
                        context,
                        input,
                        Cons.Items(a[1]).ToHashSet(),
                        complement,
                        direction == StringTextAtoms.Trailing
                    );
                }
            case StringFunctionNames.Lexemes:
            case StringFunctionNames.NthLexeme:
                {
                    Term separators = a[^1];
                    int number = function == StringFunctionNames.NthLexeme ? StringLegacy.Count(a[1], 1) : 0;

                    return await StringLexemes.Run(
                        context,
                        input,
                        separators,
                        function == StringFunctionNames.NthLexeme ? number : null
                    );
                }
            case StringFunctionNames.Find:
            case StringFunctionNames.Split:
            case StringFunctionNames.Replace:
                {
                    bool replace = function == StringFunctionNames.Replace;
                    int whereIndex = replace ? 3 : 2;
                    string where = a.Count > whereIndex && a[whereIndex] is Atom atom ? atom.Name : a.Count == whereIndex ? StringTextAtoms.Leading : throw StringLegacy.Clause();
                    if (StringUnicode.IsEmpty(a[1]))
                    {
                        return function == StringFunctionNames.Find ? input : Term.List(input);
                    }
                    if (where is not (StringTextAtoms.Leading or StringTextAtoms.Trailing or StringTextAtoms.All) || function == StringFunctionNames.Find && where == StringTextAtoms.All)
                    {
                        throw StringLegacy.Clause();
                    }
                    var needle = await StringUnicode.Elements(context, a[1], false);
                    if (function == StringFunctionNames.Find)
                    {
                        return await StringPatternSearch.Find(
                            context,
                            input,
                            needle,
                            where == StringTextAtoms.Trailing
                        );
                    }
                    Term split = await StringPatternSearch.Split(
                        context,
                        input,
                        needle,
                        where
                    );
                    var pieces = Cons.Items(split).ToList();
                    if (replace && pieces.Count > 1)
                    {
                        var joined = new List<Term>();
                        foreach (Term piece in pieces)
                        {
                            if (joined.Count > 0)
                            {
                                joined.Add(a[2]);
                            }
                            joined.Add(piece);
                        }

                        return Cons.From(joined);
                    }

                    return Cons.From(pieces);
                }
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static ValueTask<Term> Whitespace(ProcessContext context)
        => context.Runtime.Modules.Call(context, StringUnicodeDependencyNames.Module, StringUnicodeDependencyNames.PatternWhitespace);

    private static async ValueTask<Term?> Prefix(ProcessContext context, Term input, IReadOnlyList<Term> needle)
    {
        Term tail = input;
        for (int index = 0; index < needle.Count; index++)
        {
            Term result = await StringUnicode.Utility(context, index == needle.Count - 1 ? StringUnicodeDependencyNames.Grapheme : StringUnicodeDependencyNames.Codepoint, tail);
            Cons? next = result as Cons;
            if (next is null || !next.Head.Equals(needle[index]))
            {
                return null;
            }
            tail = next.Tail;
        }

        return tail is Nil && input is BitString ? StringUnicode.Empty(input) : tail;
    }

    private static async ValueTask<Term> Take(
        ProcessContext context,
        Term input,
        HashSet<Term> separators,
        bool complement,
        bool trailing
    )
    {
        if (separators.Count == 0)
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

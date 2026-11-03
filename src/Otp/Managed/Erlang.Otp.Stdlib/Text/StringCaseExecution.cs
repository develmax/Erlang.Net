namespace Erlang;

internal static class StringCaseExecution
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term input, string function)
    {
        if (function == StringFunctionNames.Titlecase)
        {
            if (input is not (Cons or Nil or BitString { IsBinary: true }))
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    [input],
                    StringCaseSource.TitleHead
                );
            }
            Term mapped;
            using (StringSourceTrace.Scope(
                context,
                function,
                1,
                input is BitString ? StringCaseSource.TitleBinary : StringCaseSource.TitleList
            ))
            {
                mapped = await StringUnicode.Utility(context, function, input);
            }
            if (mapped is not Cons cell)
            {
                return input is BitString ? StringUnicode.Empty(input) : mapped;
            }

            if (input is BitString && cell.Tail is BitString suffix)
            {
                BitString head = (BitString)await StringUnicode.Encode(context, input, cell.Head is Integer ? [cell.Head] : Cons.Items(cell.Head));

                return new BitString(head.ToArray().Concat(suffix.ToArray()).ToArray());
            }

            return StringUnicode.AppendCharacter(cell.Head, cell.Tail);
        }
        StringCaseSource source = function == StringFunctionNames.Uppercase ? StringCaseSource.Upper
            : function == StringFunctionNames.Lowercase ? StringCaseSource.Lower : StringCaseSource.Fold;
        if (input is Cons or Nil)
        {
            using var call = StringSourceTrace.Scope(
                context,
                function,
                1,
                source.PublicList
            );

            return await List(context, input, source);
        }
        if (input is BitString { IsBinary: true } binary)
        {
            if (binary.BitLength == 0)
            {
                return binary;
            }
            if (StringModernExecution.Decode(binary, out Integer cp, out BitString rest))
            {
                (bool changed, List<Term> values) result;
                using (StringSourceTrace.Scope(
                    context,
                    function,
                    1,
                    source.PublicBinary
                ))
                {
                    result = await Binary(
                        context,
                        cp,
                        rest,
                        source
                    );
                }
                if (!result.changed)
                {
                    return binary;
                }
                using var encode = StringSourceTrace.Scope(
                    context,
                    function,
                    1,
                    source.PublicEncode
                );

                return await StringUnicode.Encode(context, input, result.values);
            }
        }

        throw StringSourceTrace.Error(
            context,
            Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), input),
            function,
            1,
            source.Error
        );
    }

    private static bool AsciiChange(Integer cp, StringCaseSource source)
        => source == StringCaseSource.Upper ? cp.Value >= 'a' && cp.Value <= 'z' : cp.Value >= 'A' && cp.Value <= 'Z';

    private static Term AsciiResult(Integer cp, StringCaseSource source)
        => new Integer(cp.Value + (source == StringCaseSource.Upper ? -32 : 32));

    private static async ValueTask<Term> List(ProcessContext context, Term original, StringCaseSource source)
    {
        Term input = original;
        bool changed = false;
        var output = new List<Term>();
        var pending = new List<IDisposable>();
        int? previousLine = null;
        try
        {
            while (true)
            {
                if (input is Nil)
                {
                    return changed ? Cons.From(output) : original;
                }
                int recursion;
                if (input is Cons { Head: Integer cp, Tail: Cons { Head: Integer second } } cell
                    && second.Value >= 0 && second.Value < 256
                    && (AsciiChange(cp, source) || cp.Value >= 0 && cp.Value < 128))
                {
                    bool maps = AsciiChange(cp, source);
                    output.Add(maps ? AsciiResult(cp, source) : cp);
                    changed |= maps;
                    input = cell.Tail;
                    recursion = source.ListCall + (maps ? StringCaseSource.ListAsciiChangedOffset : StringCaseSource.ListAsciiUnchangedOffset);
                }
                else
                {
                    Term mapped;
                    using (StringSourceTrace.Scope(
                        context,
                        source.ListHelper,
                        2,
                        source.ListCall
                    ))
                    {
                        mapped = await StringUnicode.Utility(context, source.Function, input);
                    }
                    if (mapped is Nil)
                    {
                        return changed ? Cons.From(output) : original;
                    }
                    if (mapped is not Cons next)
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), mapped),
                            source.ListHelper,
                            2,
                            source.ListCall
                        );
                    }
                    bool same = input is Cons before && before.Head.Equals(next.Head);
                    changed |= !same;
                    output.AddRange(next.Head is Integer ? [next.Head] : Cons.Items(next.Head));
                    input = next.Tail;
                    recursion = source.ListCall + (same ? StringCaseSource.ListUnchangedOffset : StringCaseSource.ListChangedOffset);
                }
                if (recursion != previousLine)
                {
                    pending.Add(StringSourceTrace.Scope(
                        context,
                        source.ListHelper,
                        2,
                        recursion
                    ));
                }
                previousLine = recursion;
                await context.ReduceAsync();
            }
        }
        finally
        {
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                pending[index].Dispose();
            }
        }
    }

    private static async ValueTask<(bool changed, List<Term> values)> Binary(
        ProcessContext context,
        Integer cp,
        BitString rest,
        StringCaseSource source
    )
    {
        bool changed = false;
        var output = new List<Term>();
        var pending = new List<IDisposable>();
        int? previousLine = null;
        try
        {
            while (true)
            {
                int recursion;
                if (StringModernExecution.Decode(rest, out Integer next, out BitString following)
                    && next.Value < 256 && (AsciiChange(cp, source) || cp.Value >= 0 && cp.Value < 128))
                {
                    bool maps = AsciiChange(cp, source);
                    output.Add(maps ? AsciiResult(cp, source) : cp);
                    changed |= maps;
                    cp = next;
                    rest = following;
                    recursion = source.BinaryCall + (maps ? StringCaseSource.BinaryAsciiChangedOffset : StringCaseSource.BinaryAsciiUnchangedOffset);
                }
                else
                {
                    Term mapped;
                    using (StringSourceTrace.Scope(
                        context,
                        source.BinaryHelper,
                        3,
                        source.BinaryCall
                    ))
                    {
                        mapped = await StringUnicode.Utility(context, source.Function, new Cons(cp, rest));
                    }
                    if (mapped is not Cons cell)
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), mapped),
                            source.BinaryHelper,
                            3,
                            source.BinaryCall
                        );
                    }
                    bool same = cell.Head.Equals(cp);
                    changed |= !same;
                    output.Add(cell.Head);
                    Term point;
                    using (StringSourceTrace.Scope(
                        context,
                        source.BinaryHelper,
                        3,
                        source.BinaryCall + (same ? StringCaseSource.BinaryUnchangedCpOffset : StringCaseSource.BinaryChangedCpOffset)
                    ))
                    {
                        point = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, cell.Tail);
                    }
                    if (point is Nil)
                    {
                        return (changed, output);
                    }
                    if (point is not Cons { Head: Integer first, Tail: BitString remaining })
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), ((TupleTerm)point).Items[1]),
                            source.BinaryHelper,
                            3,
                            source.BinaryCall + (same ? StringCaseSource.BinaryUnchangedErrorOffset : StringCaseSource.BinaryChangedErrorOffset)
                        );
                    }
                    cp = first;
                    rest = remaining;
                    recursion = source.BinaryCall + (same ? StringCaseSource.BinaryUnchangedRecursionOffset : StringCaseSource.BinaryChangedRecursionOffset);
                }
                if (recursion != previousLine)
                {
                    pending.Add(StringSourceTrace.Scope(
                        context,
                        source.BinaryHelper,
                        3,
                        recursion
                    ));
                }
                previousLine = recursion;
                await context.ReduceAsync();
            }
        }
        finally
        {
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                pending[index].Dispose();
            }
        }
    }
}

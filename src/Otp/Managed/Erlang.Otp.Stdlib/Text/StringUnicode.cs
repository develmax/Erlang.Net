using System.Text;

namespace Erlang;

internal static class StringUnicode
{
    internal static bool IsEmpty(Term input)
    {
        var pending = new Stack<Term>();
        pending.Push(input);
        while (pending.TryPop(out Term? value))
        {
            switch (value)
            {
                case Nil:
                case BitString { BitLength: 0 }:
                    break;
                case Cons cell:
                    pending.Push(cell.Tail);
                    pending.Push(cell.Head);
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    internal static ValueTask<Term> Utility(ProcessContext context, string operation, Term input)
        => context.Runtime.Modules.Call(
            context,
            StringUnicodeDependencyNames.Module,
            operation,
            input
        );

    internal static async ValueTask<Cons?> Next(ProcessContext context, Term input, bool grapheme)
    {
        Term result = await Utility(context, grapheme ? StringUnicodeDependencyNames.Grapheme : StringUnicodeDependencyNames.Codepoint, input);
        if (result is Cons cell)
        {
            return cell;
        }
        if (result is Nil)
        {
            return null;
        }
        if (result is TupleTerm { Items.Count: 2 } error && error.Items[0].Equals(Term.A(CommonAtomNames.Error)))
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), error.Items[1]));
        }

        throw StringLegacy.Clause();
    }

    internal static async ValueTask<Cons?> Next(
        ProcessContext context,
        Term input,
        bool grapheme,
        StringSourceStep source
    )
    {
        Term result;
        using (StringSourceTrace.Scope(
            context,
            source.Function,
            source.Arity,
            source.CallLine
        ))
        {
            result = await Utility(context, grapheme ? StringUnicodeDependencyNames.Grapheme : StringUnicodeDependencyNames.Codepoint, input);
        }
        if (result is Cons cell)
        {
            return cell;
        }
        if (result is Nil)
        {
            return null;
        }
        if (result is TupleTerm { Items.Count: 2 } error && error.Items[0].Equals(Term.A(CommonAtomNames.Error)))
        {
            throw StringSourceTrace.Error(
                context,
                source.CaseClauseOnError
                ? Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result)
                : Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), error.Items[1]),
                source.Function,
                source.Arity,
                source.ErrorLine
            );
        }

        throw StringSourceTrace.Error(
            context,
            Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result),
            source.Function,
            source.Arity,
            source.CallLine
        );
    }

    internal static async ValueTask<List<Term>> Elements(ProcessContext context, Term input, bool grapheme)
    {
        var result = new List<Term>();
        while (await Next(context, input, grapheme) is Cons cell)
        {
            result.Add(cell.Head);
            input = cell.Tail;
            await context.ReduceAsync();
        }

        return result;
    }

    internal static Term Empty(Term input) => input is BitString { IsBinary: true } ? new BitString([]) : Nil.Value;

    internal static Term AppendCharacter(Term character, Term tail)
    {
        if (tail is BitString { BitLength: 0 })
        {
            return character is Integer ? Term.List(character) : character;
        }
        if (tail is BitString)
        {
            return Term.List(character, tail);
        }

        return character is Integer ? new Cons(character, tail) : StringLegacy.Append(character, tail);
    }

    internal static async ValueTask<Term> Encode(ProcessContext context, Term original, IEnumerable<Term> codepoints)
    {
        Term list = Cons.From(codepoints);
        if (original is not BitString)
        {
            return list;
        }

        return await context.Runtime.Modules.Call(
            context,
            UnicodeNativeNames.Module,
            UnicodeNativeNames.ToBinary,
            list
        );
    }

    internal static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> a)
    {
        Term input = a[0];
        switch (function)
        {
            case StringFunctionNames.IsEmpty:
                return Term.A(IsEmpty(input) ? CommonAtomNames.True : CommonAtomNames.False);
            case StringFunctionNames.NextCodepoint:
            case StringFunctionNames.NextGrapheme:
                return await Utility(
                    context,
                    function == StringFunctionNames.NextCodepoint ? StringUnicodeDependencyNames.Codepoint : StringUnicodeDependencyNames.Grapheme,
                    input
                );
            case StringFunctionNames.Length:
                return await StringModernExecution.LengthOrReverse(context, input, false);
            case StringFunctionNames.ToGraphemes:
                return await StringModernExecution.Graphemes(context, input);
            case StringFunctionNames.Reverse:
                return await StringModernExecution.LengthOrReverse(context, input, true);
            case StringFunctionNames.Slice:
                return await StringModernExecution.Slice(context, a);
            case StringFunctionNames.Pad:
                return await StringModernExecution.Pad(context, a);
            case StringFunctionNames.Equal:
                return await StringComparisonExecution.Equal(context, a);
            case StringFunctionNames.Uppercase:
            case StringFunctionNames.Lowercase:
            case StringFunctionNames.Casefold:
            case StringFunctionNames.Titlecase:
                return await StringCaseExecution.Run(context, input, function);
            case StringFunctionNames.ToFloat:
            case StringFunctionNames.ToInteger:
                return await Number(context, input, function == StringFunctionNames.ToFloat);
            case StringFunctionNames.ListToFloat:
                return await context.Runtime.Modules.Call(
                    context,
                    StringModule.Name,
                    function,
                    input
                );
            case StringFunctionNames.JaroSimilarity:
                return await Similarity(context, input, a[1]);
            default:
                return await StringSearch.Invoke(context, function, a);
        }
    }

    private static async ValueTask<Term> Number(ProcessContext context, Term input, bool floating)
    {
        try
        {
            Term allowed = Term.String(floating ? "+-0123456789eE.," : "+-0123456789");
            var pair = (TupleTerm)await StringSearch.Invoke(context, StringFunctionNames.Take, [input, allowed]);
            if (IsEmpty(pair.Items[0]))
            {
                return Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(floating ? StringTextAtoms.NoFloat : StringTextAtoms.NoInteger));
            }
            Term list = await context.Runtime.Modules.Call(
                context,
                UnicodeNativeNames.Module,
                UnicodeNativeNames.ToList,
                pair.Items[0]
            );
            Term parsed = await context.Runtime.Modules.Call(
                context,
                StringModule.Name,
                floating ? StringFunctionNames.ListToFloat : StringFunctionNames.ListToInteger,
                list
            );
            var result = (TupleTerm)parsed;
            if (result.Items[0].Equals(Term.A(CommonAtomNames.Error)))
            {
                return result;
            }
            Term remainder;
            if (input is BitString binary)
            {
                int consumed = Cons.Items(list).Count() - Cons.Items(result.Items[1]).Count();
                remainder = binary.SliceBits(consumed * 8, binary.BitLength - consumed * 8);
            }
            else
            {
                remainder = StringLegacy.Append(result.Items[1], pair.Items[1]);
            }

            return Term.Tuple(result.Items[0], remainder);
        }
        catch (ErlangException)
        {
            return Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(ErlangErrorReasons.BadArgument));
        }
    }

    private static async ValueTask<Term> Similarity(ProcessContext context, Term first, Term second)
    {
        List<Term> left = await StringComparisonExecution.SimilarityElements(context, first, false);
        List<Term> right = await StringComparisonExecution.SimilarityElements(context, second, true);
        if (left.Count == 0 || right.Count == 0)
        {
            return new FloatTerm(left.Count == right.Count ? 1.0 : 0.0);
        }
        int distance = Math.Max(1, Math.Max(left.Count, right.Count) / 2);
        var positions = new Dictionary<Term, Queue<int>>();
        for (int index = 0; index < right.Count; index++)
        {
            if (!positions.TryGetValue(right[index], out Queue<int>? indexes))
            {
                indexes = new Queue<int>();
                positions.Add(right[index], indexes);
            }
            indexes.Enqueue(index);
            await context.ReduceAsync();
        }
        var matched = new bool[right.Count];
        var leftMatches = new List<Term>();
        for (int index = 0; index < left.Count; index++)
        {
            if (positions.TryGetValue(left[index], out Queue<int>? indexes))
            {
                // Expired positions cannot enter any later source matching window.
                while (indexes.TryPeek(out int expired) && expired <= index - distance)
                {
                    indexes.Dequeue();
                }
                if (indexes.TryPeek(out int candidate) && candidate < index + distance)
                {
                    indexes.Dequeue();
                    matched[candidate] = true;
                    leftMatches.Add(left[index]);
                }
            }
            await context.ReduceAsync();
        }
        if (leftMatches.Count == 0)
        {
            return new FloatTerm(0.0);
        }
        var rightMatches = right.Where((_, index) => matched[index]).ToArray();
        int transpositions = leftMatches.Where((value, index) => !value.Equals(rightMatches[index])).Count();
        double count = leftMatches.Count;

        return new FloatTerm((count / left.Count + count / right.Count + (count - transpositions / 2.0) / count) / 3.0);
    }
}

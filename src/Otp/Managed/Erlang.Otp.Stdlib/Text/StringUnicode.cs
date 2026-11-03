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

    internal static Term Empty(Term input) => input is BitString ? new BitString([]) : Nil.Value;

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
                return Term.I((await Elements(context, input, true)).Count);
            case StringFunctionNames.ToGraphemes:
                return Cons.From(await Elements(context, input, true));
            case StringFunctionNames.Reverse:
                {
                    var elements = await Elements(context, input, true);
                    elements.Reverse();

                    return Cons.From(elements);
                }
            case StringFunctionNames.Slice:
                return await Slice(
                    context,
                    input,
                    a[1],
                    a.Count == 3 ? a[2] : Term.A(StringTextAtoms.Infinity)
                );
            case StringFunctionNames.Pad:
                {
                    var length = StringLegacy.IntegerValue(a[1]);
                    string direction = a.Count > 2 && a[2] is Atom atom ? atom.Name : a.Count == 2 ? StringTextAtoms.Trailing : throw StringLegacy.Clause();
                    Term fill = a.Count == 4 ? a[3] : Term.I(' ');
                    int actual = (await Elements(context, input, true)).Count;
                    int missing = StringLegacy.Count(new Integer(System.Numerics.BigInteger.Max(0, length - actual)));
                    Term padding = Cons.From(Enumerable.Repeat(fill, missing));
                    switch (direction)
                    {
                        case StringTextAtoms.Leading:
                            return Term.List(padding, input);
                        case StringTextAtoms.Trailing:
                            return new Cons(input, padding);
                        case StringTextAtoms.Both:
                            {
                                Term half = Cons.From(Enumerable.Repeat(fill, missing / 2));

                                return Cons.From([half, input, half], missing % 2 == 0 ? Nil.Value : Term.List(fill));
                            }
                        default:
                            throw StringLegacy.Clause();
                    }
                }
            case StringFunctionNames.Equal:
                return await Equal(context, a);
            case StringFunctionNames.Uppercase:
            case StringFunctionNames.Lowercase:
            case StringFunctionNames.Casefold:
            case StringFunctionNames.Titlecase:
                return await ChangeCase(context, input, function);
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

    private static async ValueTask<Term> Slice(
        ProcessContext context,
        Term input,
        Term startValue,
        Term lengthValue
    )
    {
        if (lengthValue.Equals(Term.I(0)))
        {
            return Empty(input);
        }
        int start = StringLegacy.Count(startValue);
        int? length = lengthValue.Equals(Term.A(StringTextAtoms.Infinity)) ? null : StringLegacy.Count(lengthValue, 1);
        Term tail = input;
        for (int index = 0; index < start; index++)
        {
            Cons? next = await Next(context, tail, true);
            if (next is null)
            {
                return Empty(input);
            }
            tail = next.Tail;
        }
        if (length is null)
        {
            return tail is Nil && input is BitString ? Empty(input) : tail;
        }
        var selected = new List<Term>();
        Term remaining = tail;
        for (int index = 0; index < length; index++)
        {
            Cons? next = await Next(context, remaining, true);
            if (next is null)
            {
                break;
            }
            if (next.Head is Integer)
            {
                selected.Add(next.Head);
            }
            else
            {
                selected.AddRange(Cons.Items(next.Head));
            }
            remaining = next.Tail;
        }

        return await Encode(context, tail, selected);
    }

    private static async ValueTask<Term> ChangeCase(ProcessContext context, Term input, string operation)
    {
        if (input is not (Cons or Nil or BitString { IsBinary: true }))
        {
            if (operation == StringFunctionNames.Titlecase)
            {
                throw StringLegacy.Clause();
            }
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), input));
        }
        if (operation == StringFunctionNames.Titlecase)
        {
            Term mapped = await Utility(context, operation, input);
            if (mapped is not Cons cell)
            {
                return input is BitString ? Empty(input) : mapped;
            }
            if (input is BitString)
            {
                return await context.Runtime.Modules.Call(
                    context,
                    UnicodeNativeNames.Module,
                    UnicodeNativeNames.ToBinary,
                    mapped
                );
            }

            return AppendCharacter(cell.Head, cell.Tail);
        }

        Term tail = input;
        var output = new List<Term>();
        bool changed = false;
        while (true)
        {
            Term mapped = await Utility(context, operation, tail);
            if (mapped is Nil)
            {
                break;
            }
            if (mapped is not Cons cell)
            {
                throw StringLegacy.Clause();
            }
            Term? originalHead = tail is Cons originalCell ? originalCell.Head : (await Next(context, tail, false))?.Head;
            changed |= originalHead is null || !originalHead.Equals(cell.Head);
            if (cell.Head is Integer)
            {
                output.Add(cell.Head);
            }
            else
            {
                output.AddRange(Cons.Items(cell.Head));
            }
            tail = cell.Tail;
            await context.ReduceAsync();
        }

        return changed ? await Encode(context, input, output) : input;
    }

    private static async ValueTask<Term> Equal(ProcessContext context, IReadOnlyList<Term> a)
    {
        bool fold = a.Count > 2 && a[2].Equals(Term.A(CommonAtomNames.True));
        if (a.Count > 2 && !fold && !a[2].Equals(Term.A(CommonAtomNames.False)))
        {
            throw StringLegacy.Clause();
        }
        string normalization = a.Count == 4 && a[3] is Atom atom ? atom.Name : StringTextAtoms.NoNormalization;
        Term left = a[0];
        Term right = a[1];
        if (a.Count == 2 && left is BitString && right is BitString || (fold || normalization != StringTextAtoms.NoNormalization) && left.Equals(right))
        {
            return Term.A(left.Equals(right) ? CommonAtomNames.True : CommonAtomNames.False);
        }
        while (true)
        {
            if (normalization != StringTextAtoms.NoNormalization)
            {
                left = await Utility(context, normalization, left);
                right = await Utility(context, normalization, right);
            }
            if (fold)
            {
                left = await Utility(context, StringUnicodeDependencyNames.Casefold, left);
                right = await Utility(context, StringUnicodeDependencyNames.Casefold, right);
            }
            Cons? l = await Next(context, left, false);
            Cons? r = await Next(context, right, false);
            if (l is null || r is null)
            {
                return Term.A(l is null && r is null ? CommonAtomNames.True : CommonAtomNames.False);
            }
            if (!l.Head.Equals(r.Head))
            {
                return Term.A(CommonAtomNames.False);
            }
            left = l.Tail;
            right = r.Tail;
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
            List<Term> head = await Elements(context, pair.Items[0], false);
            Term list = Cons.From(head);
            Term parsed = floating
                ? await context.Runtime.Modules.Call(
                    context,
                    StringModule.Name,
                    StringFunctionNames.ListToFloat,
                    list
                )
                : StringLegacy.Invoke(StringFunctionNames.ListToInteger, [list]);
            var result = (TupleTerm)parsed;
            if (result.Items[0].Equals(Term.A(CommonAtomNames.Error)))
            {
                return result;
            }
            Term remainder;
            if (input is BitString binary)
            {
                int consumed = head.Count - Cons.Items(result.Items[1]).Count();
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
        List<Term> left = await Elements(context, first, true);
        List<Term> right = await Elements(context, second, true);
        if (left.Count == 0 || right.Count == 0)
        {
            return new FloatTerm(left.Count == right.Count ? 1.0 : 0.0);
        }
        int distance = Math.Max(1, Math.Max(left.Count, right.Count) / 2);
        var matched = new bool[right.Count];
        var leftMatches = new List<Term>();
        for (int index = 0; index < left.Count; index++)
        {
            for (int candidate = Math.Max(0, index - distance + 1); candidate < Math.Min(right.Count, index + distance); candidate++)
            {
                if (!matched[candidate] && left[index].Equals(right[candidate]))
                {
                    matched[candidate] = true;
                    leftMatches.Add(left[index]);
                    break;
                }
            }
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

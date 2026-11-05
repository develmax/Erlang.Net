using System.Numerics;

namespace Erlang;

internal static class StringLegacyExecution
{
    internal static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> a)
    {
        switch (function)
        {
            case StringFunctionNames.Len:
                {
                    using var call = StringSourceTrace.Scope(
                        context,
                        function,
                        1,
                        StringLegacySource.Len
                    );

                    return await context.Runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        StringFunctionNames.Length,
                        a[0]
                    );
                }
            case StringFunctionNames.Concat:
                {
                    using var call = StringSourceTrace.Scope(
                        context,
                        function,
                        2,
                        StringLegacySource.Concat
                    );

                    return await Append(context, a[0], a[1]);
                }
            case StringFunctionNames.Substr:
            case StringFunctionNames.SubString:
                return StringLegacySubstringExecution.Invoke(context, function, a);
            case StringFunctionNames.Chr:
            case StringFunctionNames.Rchr:
            case StringFunctionNames.Str:
            case StringFunctionNames.Rstr:
            case StringFunctionNames.Span:
            case StringFunctionNames.Cspan:
            case StringFunctionNames.Tokens:
                return await StringLegacySearchExecution.Invoke(
                    context,
                    function,
                    a[0],
                    a[1]
                );
            case StringFunctionNames.Strip:
                return await StringLegacyWordExecution.Strip(context, a);
            case StringFunctionNames.Words:
            case StringFunctionNames.SubWord:
            case StringFunctionNames.Left:
            case StringFunctionNames.Right:
            case StringFunctionNames.Centre:
                return await StringLegacyWordExecution.Invoke(context, function, a);
            case StringFunctionNames.Chars:
                return await Chars(
                    context,
                    a[0],
                    a[1],
                    a.Count == 3 ? a[2] : Nil.Value
                );
            case StringFunctionNames.Copies:
                {
                    if (a[0] is not (Cons or Nil) || a[1] is not Integer { Value.Sign: >= 0 } count)
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            a,
                            StringLegacySource.Copies
                        );
                    }
                    Term result = Nil.Value;
                    for (BigInteger remaining = count.Value; remaining > 0; remaining--)
                    {
                        using var call = StringSourceTrace.Scope(
                            context,
                            function,
                            3,
                            StringLegacySource.CopiesAppend
                        );
                        result = await Append(context, a[0], result);
                        await context.ReduceAsync();
                    }

                    return result;
                }
            case StringFunctionNames.ToUpper:
            case StringFunctionNames.ToLower:
                return await LatinCase(context, function, a[0]);
            case StringFunctionNames.ListToInteger:
                return StringLegacy.ReadInteger(a[0]);
            case StringFunctionNames.Join:
                {
                    if (a[0] is Nil && a[1] is Cons or Nil)
                    {
                        return Nil.Value;
                    }
                    if (a[0] is not Cons first)
                    {
                        throw StringSourceTrace.Clause(
                            context,
                            function,
                            a,
                            StringLegacySource.Join
                        );
                    }
                    var parts = new List<Term>();
                    using var call = StringSourceTrace.Scope(
                        context,
                        function,
                        2,
                        StringLegacySource.JoinBody
                    );
                    Term remaining = first.Tail;
                    while (remaining is Cons entry)
                    {
                        using var generator = StringSourceTrace.Scope(
                            context,
                            StringSourceNames.JoinComprehension,
                            2,
                            StringLegacySource.JoinBody
                        );
                        parts.Add(await Append(context, a[1], entry.Head));
                        remaining = entry.Tail;
                    }
                    if (remaining is not Nil)
                    {
                        throw StringSourceTrace.Error(
                            context,
                            Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), remaining),
                            StringSourceNames.JoinComprehension,
                            2,
                            StringLegacySource.JoinBody
                        );
                    }
                    Term tail = await context.Runtime.Modules.Call(
                        context,
                        ListModuleNames.Module,
                        ListModuleNames.Append,
                        Cons.From(parts)
                    );

                    return await Append(context, first.Head, tail);
                }
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    internal static ValueTask<Term> Append(ProcessContext context, Term left, Term right)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            StringSourceNames.AppendOperator,
            left,
            right
        );

    internal static async ValueTask<Term> Chars(
        ProcessContext context,
        Term character,
        Term number,
        Term tail
    )
    {
        if (number is not Integer count || count.Value < 0)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Chars,
                [character, number, tail],
                StringLegacySource.Chars
            );
        }
        BigInteger remaining = count.Value;
        while (remaining > 0)
        {
            tail = new Cons(character, tail);
            remaining--;
            await context.ReduceAsync();
        }
        if (character is not Integer)
        {
            throw StringSourceTrace.Clause(
                context,
                StringFunctionNames.Chars,
                [character, Term.I(0), tail],
                StringLegacySource.Chars
            );
        }

        return tail;
    }

    private static async ValueTask<Term> LatinCase(ProcessContext context, string function, Term input)
    {
        bool upper = function == StringFunctionNames.ToUpper;
        if (input is Integer)
        {
            return StringLegacy.LatinCase(input, upper);
        }
        if (input is not (Cons or Nil))
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                [input],
                upper ? StringLegacySource.Upper : StringLegacySource.Lower
            );
        }
        var values = new List<Term>();
        while (input is Cons cell)
        {
            values.Add(StringLegacy.LatinCase(cell.Head, upper));
            input = cell.Tail;
            await context.ReduceAsync();
        }
        if (input is not Nil)
        {
            string helper = upper ? StringSourceNames.UpperComprehension : StringSourceNames.LowerComprehension;
            int line = upper ? StringLegacySource.UpperComprehension : StringLegacySource.LowerComprehension;
            throw StringSourceTrace.Error(
                context,
                Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), input),
                helper,
                1,
                line,
                values.Count == 0 ? [] : [StringSourceTrace.Frame(helper, 1, line)]
            );
        }

        return Cons.From(values);
    }
}

namespace Erlang;

// Algorithms ported from the unchanged OTP29.1.1 generated unicode_util.erl.
// Copyright Ericsson AB 2017-2026. Apache-2.0; original sources/notices retained.
public static class UnicodeUtilModule
{
    public const string Name = UnicodeUtilNames.Module;
    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        ("cp",1), ("gc",1), ("nfd",1), ("nfc",1), ("nfkd",1), ("nfkc",1),
        ("pattern_whitespace",0), ("is_whitespace",1), ("uppercase",1), ("lowercase",1),
        ("titlecase",1), ("casefold",1), ("spec_version",0), ("lookup",1), ("category",1),
        ("get_case",1), ("is_wide",1), ("is_other_id_start",1), ("is_other_id_continue",1),
        ("is_letter_not_pattern_syntax",1)
    });

    public static void Register(ModuleRegistry registry) => registry.InstallPreloaded(target =>
    {
        foreach (var export in Exports)
        {
            string function = export.Name;
            target.RegisterReviewedManagedLibrary789(
                Name,
                function,
                export.Arity,
                (context, args) => Invoke(context, function, args)
            );
        }
    });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count))) throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        using var tail = context.EnterSourceTailCall(Name, function);
        await context.BumpReductionsAsync(1);
        if (function == "spec_version") return Term.Tuple(Term.I(UnicodeUtilNames.VersionMajor), Term.I(UnicodeUtilNames.VersionMinor));
        if (function == "pattern_whitespace") return Term.List(
            Term.List(Term.I(13), Term.I(10)),
            Term.I(9),
            Term.I(10),
            Term.I(11),
            Term.I(12),
            Term.I(13),
            Term.I(32),
            Term.I(133),
            Term.I(8206),
            Term.I(8207),
            Term.I(8232),
            Term.I(8233)
        );
        Term input = arguments[0];
        if (function == "cp") return await UnicodeUtilTraversal.Cp(context, input);
        if (function == "gc") return await UnicodeUtilGraphemes.Gc(context, input);
        if (function is "nfd" or "nfc" or "nfkd" or "nfkc") return await UnicodeUtilNormalization.Normalize(context, function, input);
        if (function is "uppercase" or "lowercase" or "titlecase" or "casefold")
        {
            int callLine = function == "uppercase" ? UnicodeUtilSourceLines.UppercaseRead : function == "lowercase" ? UnicodeUtilSourceLines.LowercaseRead : function == "titlecase" ? UnicodeUtilSourceLines.TitlecaseRead : UnicodeUtilSourceLines.CasefoldRead;
            Term next = await UnicodeUtilTraversal.ReadAt(
                context,
                input,
                function,
                1,
                callLine
            );
            if (next is Cons cell)
            {
                int index = function == "uppercase" ? UnicodeUtilNames.UpperIndex : function == "lowercase" ? UnicodeUtilNames.LowerIndex : function == "titlecase" ? UnicodeUtilNames.TitleIndex : UnicodeUtilNames.FoldIndex;

                return new Cons(UnicodeUtilData.Instance.Case((int)((Integer)cell.Head).Value, index), cell.Tail);
            }
            if (next is Nil) return next;
            int line = function == "uppercase" ? UnicodeUtilSourceLines.UppercaseError : function == "lowercase" ? UnicodeUtilSourceLines.LowercaseError : function == "titlecase" ? UnicodeUtilSourceLines.TitlecaseError : UnicodeUtilSourceLines.CasefoldError;
            throw UnicodeUtilSource.BadArgument(
                context,
                function,
                Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), ((TupleTerm)next).Items[1]),
                line
            );
        }
        if (function == "is_whitespace") return Boolean(input.Equals(Term.List(Term.I(13), Term.I(10)))
            || UnicodeUtilTraversal.Codepoint(input, out int ws) && ws is 9 or 10 or 11 or 12 or 13 or 32 or 133 or UnicodeUtilNames.NoBreakSpace or 5760
                or >= 8192 and <= 8202 or 8232 or 8233 or 8239 or 8287 or 12288);
        if (function == "is_other_id_start") return Boolean(UnicodeUtilTraversal.Codepoint(input, out int start) && start is 6277 or 6278 or 8472 or 8494 or 12443 or 12444);
        if (function == "is_other_id_continue") return Boolean(UnicodeUtilTraversal.Codepoint(input, out int continuation)
            && continuation is 183 or 903 or 6618 or 8204 or 8205 or 12539 or 65381 or >= 4969 and <= 4977);
        if (function == "is_letter_not_pattern_syntax") return Boolean(!input.Equals(Term.I(11823)));
        if (function == "is_wide") return await Wide(context, input);
        if (!UnicodeUtilTraversal.Codepoint(input, out int cp))
        {
            throw UnicodeUtilSource.Clause(
                context,
                function,
                [input],
                function == "lookup" ? UnicodeUtilSourceLines.Lookup : function == "category" ? UnicodeUtilSourceLines.Category : UnicodeUtilSourceLines.GetCase
            );
        }
        var data = UnicodeUtilData.Instance;
        if (function == "category") return data.Category(cp);
        if (function == "lookup")
        {
            var entry = data.Lookup(cp);

            return new MapTerm([
                new(Term.A(UnicodeUtilNames.CombiningClass),Term.I(entry.Combining)),
                new(Term.A(UnicodeUtilNames.Canonical),entry.Canonical),
                new(Term.A(UnicodeUtilNames.Compatibility),entry.Compatibility),
                new(Term.A(UnicodeUtilNames.Category),data.Category(cp))]);
        }

        return new MapTerm([
            new(Term.A(UnicodeUtilNames.Upper),data.Case(cp,UnicodeUtilNames.UpperIndex)),new(Term.A(UnicodeUtilNames.Lower),data.Case(cp,UnicodeUtilNames.LowerIndex)),
            new(Term.A(UnicodeUtilNames.Title),data.Case(cp,UnicodeUtilNames.TitleIndex)),new(Term.A(UnicodeUtilNames.Fold),data.Case(cp,UnicodeUtilNames.FoldIndex))]);
    }

    private static async ValueTask<Term> Wide(ProcessContext context, Term input)
    {
        int work = 0;
        while (true)
        {
            if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
            if (UnicodeUtilTraversal.Codepoint(input, out int cp)) return Boolean(cp > UnicodeUtilNames.AsciiMaximum && UnicodeUtilData.Instance.Contains(UnicodeUtilNames.WideProperty, cp));
            if (input is Nil) return Boolean(false);
            if (input is Cons cell)
            {
                if (cell.Tail is Cons variant && variant.Head is Integer selector && (selector.Value == UnicodeUtilNames.TextPresentationSelector || selector.Value == UnicodeUtilNames.EmojiPresentationSelector)) return Boolean(true);
                if (UnicodeUtilTraversal.Codepoint(cell.Head, out cp))
                {
                    if (UnicodeUtilData.Instance.Contains(UnicodeUtilNames.WideProperty, cp)) return Boolean(true);
                    input = cell.Tail;
                    continue;
                }
            }

            throw UnicodeUtilSource.Clause(
                context,
                "is_wide",
                [input],
                UnicodeUtilSourceLines.Wide
            );
        }
    }

    private static Term Boolean(bool value) => Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
}

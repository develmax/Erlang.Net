namespace Erlang;

internal sealed class StringSeparatorSet : HashSet<Term>
{
    private Term? firstCodepoints;
    internal Term Original { get; }
    internal Term? Compiled { get; private set; }

    internal StringSeparatorSet(Term original) : base(Prefix(original))
    {
        Original = original;
    }

    private static IEnumerable<Term> Prefix(Term input)
    {
        while (input is Cons cell)
        {
            yield return cell.Head;
            input = cell.Tail;
        }
    }

    internal static async ValueTask<StringSeparatorSet> Create(
        ProcessContext context,
        Term original,
        string owner,
        int arity,
        int line
    )
    {
        var result = new StringSeparatorSet(original);
        using var caller = StringSourceTrace.Scope(
            context,
            owner,
            arity,
            line
        );
        using var pattern = StringSourceTrace.Scope(
            context,
            StringSeparatorSource.SearchPattern,
            1,
            StringSeparatorSource.PatternCodepoints
        );
        await result.Codepoints(context);

        return result;
    }

    internal async ValueTask<bool> Member(
        ProcessContext context,
        Term character,
        string owner,
        int arity,
        int line
    )
    {
        using var caller = StringSourceTrace.Scope(
            context,
            owner,
            arity,
            line
        );
        Term result = await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.Member,
            character,
            Original
        );

        return result.Equals(Term.A(CommonAtomNames.True));
    }

    internal async ValueTask<Term> Codepoints(ProcessContext context)
    {
        if (firstCodepoints is not null)
        {
            return firstCodepoints;
        }
        var values = new List<Term>();
        using var pending = new StringSourceContinuations(context, StringSeparatorSource.SearchCodepoints, 1);
        Term tail = Original;
        while (tail is Cons entry)
        {
            Term separator = entry.Head;
            tail = entry.Tail;
            if (separator is Integer)
            {
                values.Add(separator);
                pending.Push(StringSeparatorSource.CodepointIntegerRecursion);
                continue;
            }
            Term first;
            using (StringSourceTrace.Scope(
                context,
                StringSeparatorSource.SearchCodepoints,
                1,
                StringSeparatorSource.CodepointCall
            ))
            {
                first = await StringUnicode.Utility(context, StringUnicodeDependencyNames.Codepoint, separator);
            }
            if (first is not Cons cell)
            {
                throw StringSourceTrace.Error(
                    context,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), first),
                    StringSeparatorSource.SearchCodepoints,
                    1,
                    StringSeparatorSource.CodepointCall
                );
            }
            values.Add(cell.Head);
            pending.Push(StringSeparatorSource.CodepointPatternRecursion);
        }
        if (tail is not Nil)
        {
            throw StringSourceTrace.Clause(
                context,
                StringSeparatorSource.SearchCodepoints,
                [tail],
                StringSeparatorSource.CodepointClause
            );
        }
        firstCodepoints = Cons.From(values);

        return firstCodepoints;
    }

    internal async ValueTask<Term> Pattern(ProcessContext context)
        => Term.Tuple(Original, await Codepoints(context), Compiled ?? Term.A(CommonAtomNames.Undefined));

    internal async ValueTask Compile(ProcessContext context, string owner, int arity)
    {
        if (Compiled is not null)
        {
            return;
        }
        Term first = await Codepoints(context);
        ErlangExecutionFrame? current = context.CaptureExecutionChain();
        bool replace = current is { Module: StringModule.Name } && current.Function == owner && current.Arity == arity;
        using var call = context.EnterGeneratedFunction(
            StringModule.Name,
            owner,
            arity,
            StringSourceTrace.Location(StringSeparatorSource.Compile),
            this,
            replace
        );
        Compiled = await context.Runtime.Modules.Call(
            context,
            BinaryModuleNames.Module,
            BinaryModuleNames.CompilePattern,
            StringBinaryPatterns.Encode(context, first)
        );
    }

    internal static StringSeparatorSet From(HashSet<Term> values)
        => values as StringSeparatorSet ?? new StringSeparatorSet(Cons.From(values));
}

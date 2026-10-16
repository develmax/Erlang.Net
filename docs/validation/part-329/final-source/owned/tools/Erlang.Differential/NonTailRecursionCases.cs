using System.Globalization;

namespace Erlang.Differential;

public static class NonTailRecursionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Create();

    private static IReadOnlyList<CompiledModuleCase> Create()
    {
        string depth = NonTailRecursionCaseNames.FiniteDepth.ToString(CultureInfo.InvariantCulture);
        string shallow = NonTailRecursionCaseNames.ShallowDepth.ToString(CultureInfo.InvariantCulture);
        Term checksum = Term.Tuple(Term.I(NonTailRecursionCaseNames.FiniteChecksum), Term.I(NonTailRecursionCaseNames.FiniteDepth));
        Term cleanupOrder = Cons.From(Enumerable.Range(1, NonTailRecursionCaseNames.ShallowDepth).Reverse().Select(value => (Term)Term.I(value)));
        var shallowTerms = Enumerable.Range(0, NonTailRecursionCaseNames.ShallowDepth + 1).Select(value => Term.Tuple(Term.I(value), Term.I(value * (value + 1L) / 2))).ToArray();
        string shallowCalls = string.Join(
            ",",
            Enumerable.Range(0, NonTailRecursionCaseNames.ShallowDepth + 1).Select(value => "{" + value.ToString(CultureInfo.InvariantCulture) + ",walk(" + value.ToString(CultureInfo.InvariantCulture) + ")}")
        );
        Term effects = Cons.From(Enumerable.Range(1, NonTailRecursionCaseNames.ShallowDepth).Reverse().Select(value => (Term)Term.Tuple(Term.A(NonTailRecursionCaseNames.Entered), Term.I(value)))
            .Concat(Enumerable.Range(1, NonTailRecursionCaseNames.ShallowDepth).Select(value => (Term)Term.Tuple(Term.A(NonTailRecursionCaseNames.Returned), Term.I(value)))));

        return [
            Case(
                NonTailRecursionCaseNames.Named,
                "oracle_stack_named",
                "{walk(" + depth + ")," + depth + "}",
                NonTailRecursionCaseNames.NamedBody,
                checksum
            ),
            Case(
                NonTailRecursionCaseNames.Mutual,
                "oracle_stack_mutual",
                "{left(" + depth + ")," + depth + "}",
                NonTailRecursionCaseNames.MutualBody,
                checksum
            ),
            Case(
                NonTailRecursionCaseNames.Anonymous,
                "oracle_stack_anonymous",
                NonTailRecursionCaseNames.AnonymousBody + "{F(F," + depth + ")," + depth + "}",
                "",
                checksum
            ),
            Case(
                NonTailRecursionCaseNames.Tail,
                "oracle_stack_tail",
                "{walk(" + depth + ",0)," + depth + "}",
                NonTailRecursionCaseNames.TailBody,
                checksum
            ),
            Case(
                NonTailRecursionCaseNames.Shallow,
                "oracle_stack_shallow",
                "[" + shallowCalls + "]",
                NonTailRecursionCaseNames.NamedBody,
                Cons.From(shallowTerms)
            ),
            Case(
                NonTailRecursionCaseNames.Order,
                "oracle_stack_order",
                "put(events,[]),Result=walk(" + shallow + "),{Result,lists:reverse(get(events))}",
                NonTailRecursionCaseNames.OrderBody,
                Term.Tuple(Term.I(NonTailRecursionCaseNames.ShallowDepth), effects)
            ),
            Case(
                NonTailRecursionCaseNames.Cleanup,
                "oracle_stack_cleanup",
                "put(cleanups,[]),Result=walk(" + shallow + "),{Result,get(cleanups)}",
                NonTailRecursionCaseNames.CleanupBody,
                Term.Tuple(Term.I(NonTailRecursionCaseNames.ShallowChecksum), cleanupOrder)
            ),
            Case(
                NonTailRecursionCaseNames.Failure,
                "oracle_stack_failure",
                "put(cleanups,[]),E=try walk(" + shallow + ",error) catch error:boom->caught_error end,ELog=get(cleanups),put(cleanups,[]),T=try walk(" + shallow + ",throw) catch throw:boom->caught_throw end,{E,ELog,T,get(cleanups)}",
                NonTailRecursionCaseNames.FailureBody,
                Term.Tuple(
                    Term.A(NonTailRecursionCaseNames.ErrorCaught),
                    cleanupOrder,
                    Term.A(NonTailRecursionCaseNames.ThrowCaught),
                    cleanupOrder
                )
            )
        ];
    }

    private static CompiledModuleCase Case(
        string name,
        string module,
        string run,
        string functions,
        Term result
    ) => new(
        NonTailRecursionCaseNames.Prefix + name,
        "-module(" + module + "). -export([run/0]). run()->" + run + ". " + functions,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), result)
    );
}

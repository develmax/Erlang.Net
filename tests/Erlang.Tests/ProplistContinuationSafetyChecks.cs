using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class ProplistContinuationSafetyChecks
{
    internal static void Ast(ModuleDefinition module, List<ProplistContinuationSafetyResult> results)
    {
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Opaque,
            [false],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Static,
            [true],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Alias,
            [true],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Caught,
            [false],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.After,
            [false],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Captured,
            [false, true],
            results
        );
        CheckFlags(
            module,
            ProplistContinuationSafetyNames.Branch,
            [false],
            results
        );
    }

    internal static async Task Runtime(ModuleDefinition module, ProcessContext context, List<ProplistContinuationSafetyResult> results)
    {
        Term term = await Execution.InvokeAsync(
            module,
            ProplistContinuationSafetyNames.Getter,
            context,
            []
        );
        if (term is not FunctionTerm { LocalIdentity: { } identity } genuine)
        {
            throw new InvalidOperationException(ProplistContinuationSafetyNames.MissingFunction);
        }
        await Observe(
            ProplistContinuationSafetyNames.GenuineCheck,
            async () =>
        {
            await Error(
                module,
                context,
                ProplistContinuationSafetyNames.Opaque,
                [genuine],
                true
            );
        },
            results
        );
        await Observe(
            ProplistContinuationSafetyNames.ForgedCheck,
            async () =>
        {
            var forged = new FunctionTerm(
                identity,
                genuine.Arity,
                genuine.Environment,
                (_, _) => ValueTask.FromResult<Term>(Term.A(ProplistContinuationSafetyNames.HostReturned))
            );
            Term actual = await Execution.InvokeAsync(
                module,
                ProplistContinuationSafetyNames.Opaque,
                context,
                [forged]
            );
            Require(
                actual.Equals(Term.Tuple(Term.A(ProplistContinuationSafetyNames.AfterCall), Term.A(ProplistContinuationSafetyNames.HostReturned))),
                ProplistContinuationSafetyNames.WrongValue
            );
        },
            results
        );
        await Observe(
            ProplistContinuationSafetyNames.StaticCheck,
            () => Error(
                module,
                context,
                ProplistContinuationSafetyNames.Static,
                [],
                false
            ),
            results
        );
        await Observe(
            ProplistContinuationSafetyNames.AliasCheck,
            () => Error(
                module,
                context,
                ProplistContinuationSafetyNames.Alias,
                [],
                false
            ),
            results
        );
        await Observe(
            ProplistContinuationSafetyNames.CatchCheck,
            async () =>
        {
            Term actual = await Execution.InvokeAsync(
                module,
                ProplistContinuationSafetyNames.Caught,
                context,
                []
            );
            Require(actual.Equals(Term.A(ProplistContinuationSafetyNames.CaughtValue)), ProplistContinuationSafetyNames.WrongValue);
        },
            results
        );
        await Observe(
            ProplistContinuationSafetyNames.AfterCheck,
            async () =>
        {
            Term actual = await Execution.InvokeAsync(
                module,
                ProplistContinuationSafetyNames.After,
                context,
                []
            );
            Require(
                actual.Equals(Term.Tuple(Term.A(ProplistContinuationSafetyNames.CaughtValue), Term.A(ProplistContinuationSafetyNames.AfterValue))),
                ProplistContinuationSafetyNames.WrongValue
            );
        },
            results
        );
    }

    private static async Task Error(
        ModuleDefinition module,
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        bool callerExpected
    )
    {
        ErlangException? caught = null;
        try
        {
            await Execution.InvokeAsync(
                module,
                function,
                context,
                arguments
            );
        }
        catch (ErlangException error)
        {
            caught = error;
        }
        var failure = caught ?? throw new InvalidOperationException(ProplistContinuationSafetyNames.UnexpectedReturn);
        Require(
            failure.Reason.Equals(Term.A(ProplistContinuationSafetyNames.TerminalReason)),
            ProplistContinuationSafetyNames.WrongReason
        );
        bool hasCaller = Cons.Items(failure.StackTraceTerm).Any(frame => frame is TupleTerm { Items.Count: 4 } tuple
            && tuple.Items[0].Equals(Term.A(ProplistContinuationSafetyNames.Module)) && tuple.Items[1].Equals(Term.A(function)));
        Require(hasCaller == callerExpected, ProplistContinuationSafetyNames.WrongCaller);
    }

    private static void CheckFlags(
        ModuleDefinition module,
        string name,
        bool[] expected,
        List<ProplistContinuationSafetyResult> results
    )
    {
        try
        {
            var function = module.Functions.Single(value => value.Name == name);
            bool[] actual = function.Clauses.SelectMany(clause => Applies(clause.Body)).Select(value => value.CanDiscardContinuation).ToArray();
            Require(actual.SequenceEqual(expected), ProplistContinuationSafetyNames.WrongFlags);
            results.Add(new(ProplistContinuationSafetyNames.AstPrefix + name, true, null));
        }
        catch (Exception error)
        {
            results.Add(new(ProplistContinuationSafetyNames.AstPrefix + name, false, error.ToString()));
        }
    }

    private static IEnumerable<Expr.Apply> Applies(Expr expression)
    {
        if (expression is Expr.Apply apply)
        {
            yield return apply;
        }
        IEnumerable<Expr> children = expression switch
        {
            Expr.Sequence sequence => sequence.Items,
            Expr.Block block => [block.Body],
            Expr.Match match => [match.Value],
            Expr.Tuple tuple => tuple.Items,
            Expr.Call call => call.Arguments,
            Expr.Apply invocation => new[] { invocation.Function }.Concat(invocation.Arguments),
            Expr.Fun function => function.Clauses.SelectMany(ClauseExpressions),
            Expr.Case conditional => new[] { conditional.Value }.Concat(conditional.Clauses.SelectMany(ClauseExpressions)),
            Expr.Try tried => new[] { tried.Body }.Concat(tried.Clauses.SelectMany(ClauseExpressions))
                .Concat(tried.Catches.SelectMany(ClauseExpressions)).Concat(tried.After is null ? [] : new[] { tried.After }),
            Expr.Catch caught => [caught.Operand],
            _ => []
        };
        foreach (var child in children)
        {
            foreach (var nested in Applies(child))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<Expr> ClauseExpressions(Clause clause)
        => clause.Guard is null ? [clause.Body] : [clause.Guard, clause.Body];

    private static async Task Observe(string name, Func<Task> action, List<ProplistContinuationSafetyResult> results)
    {
        try
        {
            await action();
            results.Add(new(name, true, null));
        }
        catch (Exception error)
        {
            results.Add(new(name, false, error.ToString()));
        }
    }

    private static void Require(bool condition, string diagnostic)
    {
        if (!condition)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }
}

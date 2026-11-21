using AstPattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool UsesSourceInlining => sourceModule is { AutomaticInlining: true } or { InlineTargets.Count: > 0 };

    private sealed partial class GeneralEmitter
    {
        private static bool HasEarlierClosureFailureReservation(
            FunctionDefinition lexicalOwner,
            Expr.Fun selected,
            int ordinal
        )
        {
            if (ordinal != 1 || !HasPartialTupleHead(selected) || lexicalOwner.Clauses is not
                [{ Guard: null, Body: Expr.Sequence sequence } clause])
            {
                return false;
            }

            // The SSA name counter also advances for match_fail in an earlier closed fun.
            // That fun fails at its own entry label after lifting and leaves no failure stub.
            Expr.Fun? prior = null;
            bool found = false;
            foreach (Expr item in sequence.Items)
            {
                Expr expression = item is Expr.Match { Pattern: AstPattern.Variable, Value: var value } ? value : item;
                if (expression is Expr.Fun { CoreOrdinal: 0 } first && prior is null && !found)
                {
                    prior = first;
                }
                else if (prior is not null && (ReferenceEquals(expression, selected)
                    || expression is Expr.Call { DynamicRemote: false } call
                        && call.Arguments.Any(argument => ReferenceEquals(argument, selected))
                        && call.Arguments.All(argument => ReferenceEquals(argument, selected)
                            || argument is Expr.Literal or Expr.Variable)))
                {
                    found = true;
                    break;
                }
            }

            if (!found || prior is null || !HasPartialTupleHead(prior))
            {
                return false;
            }

            int definitions = 0;
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.Fun closure)
                {
                    if (!ReferenceEquals(closure, prior) && !ReferenceEquals(closure, selected))
                    {
                        return false;
                    }

                    definitions++;
                }
                else if (expression is Expr.FunctionReference or Expr.ListComprehension or Expr.BinaryComprehension
                    or Expr.MapComprehension or Expr.Try or Expr.Receive or Expr.Maybe or Expr.Apply
                    || expression is Expr.Call { DynamicRemote: true })
                {
                    return false;
                }
            }

            return definitions == 2;

            static bool HasPartialTupleHead(Expr.Fun function) => function.SelfName is null
                && function.Clauses.All(clause => clause is { Guard: null, Patterns: [AstPattern.Tuple] });
        }
    }
}

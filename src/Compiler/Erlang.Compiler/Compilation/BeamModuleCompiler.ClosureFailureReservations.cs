using AstPattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool UsesSourceInlining => sourceModule is { AutomaticInlining: true } or { InlineTargets.Count: > 0 };

    private bool HasStableListCallbackOrder =>
        !inlineListFunctions && !automaticCoreInlining && entryTupleAliasesEnabled && OnLoadFunction is null;

    private sealed partial class GeneralEmitter
    {
        private bool HasPriorListCallbackFailureReservation(
            FunctionDefinition lexicalOwner,
            Expr.Fun selected,
            int ordinal
        )
        {
            if (ordinal != 1 || !owner.HasStableListCallbackOrder
                || lexicalOwner.Clauses is not [{ Guard: null } clause]
                || !HasPartialTupleCallback(selected))
            {
                return false;
            }

            var expressions = LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk).ToArray();
            if (expressions.OfType<Expr.Fun>().ToArray() is not [var first, var second]
                || !ReferenceEquals(second, selected) || first.CoreOrdinal != 0
                || !HasPartialTupleCallback(first) || ClosureCaptures.Names(first) is not { Length: 0 }
                || expressions.Any(expression => expression is Expr.FunctionReference or Expr.ListComprehension
                    or Expr.BinaryComprehension or Expr.MapComprehension or Expr.Try or Expr.Catch or Expr.Receive
                    or Expr.Maybe or Expr.Case or Expr.If or Expr.Apply or Expr.Call { DynamicRemote: true }))
            {
                return false;
            }

            // The Core traversal reaches the closed callback's failure before the captured one,
            // even though lifting will leave its own function entry to report function_clause.
            var callbacks = LinearValueCalls(clause.Body).Where(call =>
                call is
                {
                    Function: ListModuleNames.Map or ListModuleNames.ForEach,
                    Arguments: [Expr.Fun, _]
                }
                && (call.Module == ListModuleNames.Module
                    || call.Module is null && !entries.ContainsKey((call.Function, call.Arguments.Count))
                        && owner.ResolveExternalModule(call.Function, call.Arguments.Count) == ListModuleNames.Module)
            ).ToArray();

            return callbacks is [var prior, var later]
                && ReferenceEquals(prior.Arguments[0], first) && prior.Arguments[1] is Expr.Variable
                && ReferenceEquals(later.Arguments[0], selected);

            static bool HasPartialTupleCallback(Expr.Fun callback) =>
                callback is { SelfName: null, Clauses: [{ Guard: null, Patterns: [AstPattern.Tuple] }] };

            static IEnumerable<Expr.Call> LinearValueCalls(Expr expression) => expression switch
            {
                Expr.Call call => [call],
                Expr.Sequence or Expr.List or Expr.Tuple or Expr.Block =>
                    LocalFunctionEntries.Children(expression).SelectMany(LinearValueCalls),
                Expr.Match { Pattern: AstPattern.Variable or AstPattern.Tuple, Value: var value } => LinearValueCalls(value),
                _ => []
            };
        }

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

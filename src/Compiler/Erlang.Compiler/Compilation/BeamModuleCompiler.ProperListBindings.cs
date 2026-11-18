using Erlang;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Dictionary<Expr.Case, bool>? properListCaseBindings;

        private bool HasProperListCaseBinding(Expr.Case decision) =>
            properListCaseBindings?.GetValueOrDefault(decision) == true;

        private void ReadProperListCaseBindings(FunctionDefinition function)
        {
            properListCaseBindings = null;
            if (!owner.SsaTypeInferenceEnabled)
            {
                return;
            }

            foreach (var clause in function.Clauses)
            {
                Visit(clause.Body, null);
            }

            void Visit(Expr expression, HashSet<string>? bindings)
            {
                if (expression is Expr.Case { Value: Expr.Variable selector } decision)
                {
                    properListCaseBindings ??= new(ReferenceEqualityComparer.Instance);
                    bool proven = bindings?.Contains(selector.Name) == true;
                    properListCaseBindings[decision] = properListCaseBindings.GetValueOrDefault(decision, true) && proven;
                }

                if (expression is Expr.Sequence sequence)
                {
                    HashSet<string>? sequenceBindings = null;
                    foreach (var item in sequence.Items)
                    {
                        Visit(item, sequenceBindings);
                        if (item is Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable } match
                            && (IsProperListSelector(match.Value)
                                || match.Value is Expr.Variable alias && sequenceBindings?.Contains(alias.Name) == true))
                        {
                            // A successful match establishes a fact for this binding on the rest of this sequence.
                            sequenceBindings ??= new(StringComparer.Ordinal);
                            sequenceBindings.Add(variable.Name);
                        }
                    }

                    return;
                }

                foreach (var child in LocalFunctionEntries.Children(expression))
                {
                    // A match evaluates its right-hand side in the incoming scope.
                    // Other nested expressions start independent analysis, including each branch and closure.
                    Visit(child, expression is Expr.Match match && ReferenceEquals(child, match.Value) ? bindings : null);
                }
            }
        }
    }
}


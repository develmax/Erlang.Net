namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private Dictionary<int, int> ReadLocalResultEntries(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries,
        bool provenTupleFiltersOnly = false
    )
    {
        var results = new Dictionary<int, int>();
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || OnLoadFunction is not null)
        {
            return results;
        }

        foreach (FunctionDefinition function in functions)
        {
            var signature = (function.Name, function.Arity);
            if (function.IsInlineFailure || IsDeclaredNative(signature)
                || !localEntries.TryGetValue(signature, out int label)
                || (label <= 0 && !deferredEntries.TryGetValue(label, out label)))
            {
                continue;
            }

            if (provenTupleFiltersOnly)
            {
                // The checked filter returns its proper generator tail or adds
                // a cons after recursion. Its early result flag serves a separate
                // emission case. Only the late compacted consumer needs this fact.
                if (function.IsRecursiveListMapper && function.HasProvenRecursiveTupleFilterInput)
                {
                    results.Add(MapEmittedLabel(label), ProperListTypeBits);
                }

                continue;
            }

            // Propagate the input-rooted result of recursive helpers. The existing
            // entry analysis seeds escaped functions with unknown inputs and joins
            // every returning path before certifying a proper list.
            if (function.HasProperListResult || ReadPrivateEntryFacts()?.ProvesProperListResult(signature) == true)
            {
                results.Add(MapEmittedLabel(label), ProperListTypeBits);
                continue;
            }

            if (ReadLocalAtomTupleClauses(function.Clauses) == LocalAtomOrTupleType)
            {
                results.Add(MapEmittedLabel(label), LocalAtomOrTupleType);
                continue;
            }

            bool returnsInteger = false;
            bool mixedResults = false;
            foreach (Clause clause in function.Clauses)
            {
                if (IsUnboundedIntegerClause(clause))
                {
                    returnsInteger = true;
                }
                else if (!IsKnownNonreturningLocalResult(clause.Body))
                {
                    mixedResults = true;
                    break;
                }
            }

            if (returnsInteger && !mixedResults)
            {
                results.Add(MapEmittedLabel(label), IntegerTypeBits);
            }
        }

        return results;
    }

    // These direct forms have no proven range on at least one formal input.
    // Keep bounded masks, integer literals, and guard-dependent results out of
    // this summary. Pinned beam_bounds supplies the range-free result here.
    private static bool IsUnboundedIntegerClause(Clause clause)
    {
        if (clause.Guard is not null || clause.Body is not Expr.Binary operation
            || clause.Patterns.Any(pattern => pattern is not (Pattern.Variable or Pattern.Literal { Value: Atom })))
        {
            return false;
        }

        bool left = IsFormal(operation.Left);
        bool right = IsFormal(operation.Right);

        return operation.Operator switch
        {
            ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight => right
                && (left || operation.Left is Expr.Literal { Value: Integer }),
            ErlangOperators.BitwiseAnd or ErlangOperators.Remainder => left && right,
            ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor => left && right,
            ErlangOperators.IntegerDivide => left
                && (right || operation.Right is Expr.Literal { Value: Integer divisor } && !divisor.Value.IsZero),
            _ => false
        };

        bool IsFormal(Expr expression) => expression is Expr.Variable variable
            && clause.Patterns.Any(pattern => pattern is Pattern.Variable formal && formal.Name == variable.Name);
    }

    private bool IsKnownNonreturningLocalResult(Expr expression) =>
        expression is Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false } call
        && ((call.Function, call.Arguments.Count) is (ErlangModuleNames.Error, 1 or 2 or 3)
            or (ErlangModuleNames.Exit or ErlangModuleNames.Throw, 1))
        && ResolvesErlangBif(call);
}

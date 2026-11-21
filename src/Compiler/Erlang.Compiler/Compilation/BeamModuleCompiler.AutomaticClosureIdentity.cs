namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private Dictionary<(string Name, int Arity), FunctionDefinition>? automaticClosureSourceFunctions;
    private readonly Dictionary<(string Name, int Arity), bool> automaticClosureIdentities = [];

    private readonly Dictionary<FunctionDefinition, IReadOnlyList<Expr.Fun>> predicateClosureOrder =
        new(ReferenceEqualityComparer.Instance);

    private string? ReadSsaClosureName(FunctionDefinition lexicalOwner, Expr.Fun closure, int coreOrdinal)
    {
        if (!automaticCoreInlining || sourceModule is null)
        {
            return null;
        }

        int? ordinal = ReadPredicateValueClosureOrdinal(lexicalOwner, closure);
        if (ordinal is null && UsesIndependentSsaClosureIdentity(lexicalOwner, closure, coreOrdinal))
        {
            ordinal = 0;
        }

        return ordinal is { } value
            ? FormattableString.Invariant($"-{lexicalOwner.Name}/{lexicalOwner.Arity}-anonymous-{value}-")
            : null;
    }

    private int? ReadPredicateValueClosureOrdinal(FunctionDefinition owner, Expr.Fun closure)
    {
        if (!predicateClosureOrder.TryGetValue(owner, out var order))
        {
            if (owner.Clauses is not [{ Guard: null, Body: Expr.Call or Expr.Case } entry]
                || entry.Patterns.SelectMany(LocalFunctionEntries.PatternExpressions).Any())
            {
                return null;
            }

            var predicates = new List<Expr.Fun>();
            order = ReadPredicateValueOrder(entry.Body, predicates) ? predicates : [];
            predicateClosureOrder.Add(owner, order);
        }

        for (int index = 0; index < order.Count; index++)
        {
            if (ReferenceEquals(order[index], closure))
            {
                return index;
            }
        }

        return null;
    }

    private bool ReadPredicateValueOrder(Expr expression, List<Expr.Fun> order)
    {
        if (expression is Expr.Case
            {
                Value: Expr.Call selector,
                Clauses: [var first, var second]
            }
            && first is { Guard: null, Patterns: [Pattern.Literal { Value: Atom firstKey }] }
            && second is { Guard: null, Patterns: [Pattern.Literal { Value: Atom secondKey }] }
            && firstKey.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
            && secondKey.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
            && !firstKey.Equals(secondKey)
            && ReadPredicateValueOrder(selector, order))
        {
            // Core and SSA process the selector first. Only one body can
            // introduce functions, so constructor grouping cannot reorder them.
            return first.Body is Expr.Literal ? ReadPredicateValueOrder(second.Body, order)
                : second.Body is Expr.Literal && ReadPredicateValueOrder(first.Body, order);
        }

        if (expression is Expr.Literal)
        {
            return true;
        }

        if (expression is not Expr.Call
            {
                DynamicRemote: false,
                Function: "any" or "all",
                Arguments: [Expr.Fun { SelfName: null } callback, Expr.Variable]
            } call
            || (call.Module ?? sourceModule?.CallBindings.ImportedModule(call.Function, call.Arguments.Count)) != "lists"
            || !IsClosedListPredicate(callback))
        {
            return false;
        }

        // An opaque predicate call visits the closure argument for value.
        // The closure body has no other identity or synthetic failure to reserve.
        order.Add(callback);

        return true;
    }

    private static bool IsClosedListPredicate(Expr.Fun callback) =>
        callback.Clauses is [{ Guard: null, Patterns: [Pattern.Variable], Body: var body }]
            && (IsBooleanPredicateLiteral(body)
                || body is Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable or Expr.Literal,
                    Right: Expr.Variable or Expr.Literal
                })
        || callback.Clauses is
        [
        {
            Guard: null,
            Patterns:
                [
                    Pattern.Literal { Value: Atom }
                    or Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Variable] }
                ],
            Body: var matched
        },
        { Guard: null, Patterns: [Pattern.Variable], Body: var fallback }
        ] && IsBooleanPredicateLiteral(matched) && IsBooleanPredicateLiteral(fallback);

    private static bool IsBooleanPredicateLiteral(Expr expression) =>
        expression is Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } };

    private bool UsesIndependentSsaClosureIdentity(FunctionDefinition lexicalOwner, Expr.Fun closure, int coreOrdinal)
    {
        if (!automaticCoreInlining || coreOrdinal != 0 || sourceModule is null || !IsTotalAnonymousClosure(closure))
        {
            return false;
        }

        var signature = (lexicalOwner.Name, lexicalOwner.Arity);
        if (!automaticClosureIdentities.TryGetValue(signature, out bool result))
        {
            automaticClosureSourceFunctions ??= sourceModule.DeclaredFunctions
                .ToDictionary(function => (function.Name, function.Arity));
            result = CanAssignFirstSsaAnonymousName(signature, automaticClosureSourceFunctions, sourceModule);
            automaticClosureIdentities.Add(signature, result);
        }

        return result;
    }

    private static bool CanAssignFirstSsaAnonymousName(
        (string Name, int Arity) signature,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions,
        ModuleDefinition module
    )
    {
        if (!functions.TryGetValue(signature, out var source)
            || source.Clauses.Count != 1 && !HasAtomEntryBeforeCatchAll(source.Clauses))
        {
            return false;
        }

        // The automatic inliner drops the Core name. An inlined local callee must
        // not introduce another closure, synthetic loop or helper before this one.
        bool retainsFoldCallback = RetainsOpaqueFoldCallback(source, module);
        bool found = false;
        HashSet<Expr.Apply>? formalApplications = null;
        var seen = new HashSet<(string Name, int Arity)> { signature };
        var pending = new Queue<FunctionDefinition>();
        pending.Enqueue(source);
        while (pending.TryDequeue(out var function))
        {
            HashSet<Expr.Apply>? calleeFormalApplications = null;
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.Fun candidate)
                {
                    if (!ReferenceEquals(function, source) || found || !IsTotalAnonymousClosure(candidate))
                    {
                        return false;
                    }

                    found = true;
                    formalApplications = ReadFormalCallbackApplications(candidate.Clauses[0]);
                }
                else if (expression is Expr.Apply application)
                {
                    if (ReferenceEquals(function, source))
                    {
                        if (formalApplications?.Contains(application) != true)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!retainsFoldCallback)
                        {
                            return false;
                        }

                        // The only source lambda goes to an external fold and
                        // cannot enter the local callees as a known fun value.
                        calleeFormalApplications ??= function.Clauses.SelectMany(ReadFormalCallbackApplications)
                            .ToHashSet<Expr.Apply>(ReferenceEqualityComparer.Instance);
                        if (!calleeFormalApplications.Contains(application))
                        {
                            return false;
                        }
                    }
                }
                else if (expression is Expr.FunctionReference or Expr.Try or Expr.Catch or Expr.Receive
                    or Expr.Maybe or Expr.ListComprehension or Expr.MapComprehension or Expr.BinaryComprehension
                    || expression is Expr.Call { DynamicRemote: true })
                {
                    return false;
                }
                else if (expression is Expr.Call { Module: null } call)
                {
                    var target = (call.Function, call.Arguments.Count);
                    if (target == signature)
                    {
                        return false;
                    }

                    if (functions.TryGetValue(target, out var callee) && seen.Add(target))
                    {
                        pending.Enqueue(callee);
                    }
                }
            }
        }

        return found;
    }

    private static bool RetainsOpaqueFoldCallback(FunctionDefinition function, ModuleDefinition module)
    {
        if (function.Clauses is not
        [
            {
                Body: Expr.Call { DynamicRemote: false, Arguments: [Expr.Fun callback, _, _] } call
            }
        ]
        || !IsTotalAnonymousClosure(callback))
        {
            return false;
        }

        string? target = call.Module ?? module.CallBindings.ImportedModule(call.Function, call.Arguments.Count);
        int arity = callback.Clauses[0].Patterns.Count;

        return (target, call.Function, arity) is
            ("lists", "foldl" or "foldr" or "mapfoldl" or "mapfoldr", 2)
            or ("maps", "fold", 3);
    }

    private static HashSet<Expr.Apply> ReadFormalCallbackApplications(Clause clause)
    {
        var formals = clause.Patterns.OfType<Pattern.Variable>()
            .Where(pattern => pattern.Name != VariableScopeNames.Wildcard)
            .Select(pattern => pattern.Name).ToHashSet(StringComparer.Ordinal);

        // A callback parameter has no Core value to substitute or inline.
        return LocalFunctionEntries.Walk(clause.Body).OfType<Expr.Apply>()
            .Where(application => application.Function is Expr.Variable target && formals.Contains(target.Name))
            .ToHashSet<Expr.Apply>(ReferenceEqualityComparer.Instance);
    }

    private static bool HasAtomEntryBeforeCatchAll(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [{ Guard: null } first, { Guard: null } last] || !HasUnconditionalParameters(last))
        {
            return false;
        }

        Pattern[] head = first.Patterns.ToArray();
        int selector = Array.FindIndex(head, pattern => pattern is Pattern.Literal { Value: Atom });
        if (selector < 0)
        {
            return false;
        }

        // Exhaustive source function heads retain the original MFA and do not
        // reserve a synthetic name for the caller's function-clause failure.
        head[selector] = new Pattern.Variable(VariableScopeNames.Wildcard);

        return HasUnconditionalParameters(first with { Patterns = head });
    }

    private static bool IsTotalAnonymousClosure(Expr.Fun closure) => closure.SelfName is null
        && closure.Clauses is [{ Guard: null } clause] && HasUnconditionalParameters(clause);
}

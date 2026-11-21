namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private Dictionary<(string Name, int Arity), FunctionDefinition>? automaticClosureSourceFunctions;
    private readonly Dictionary<(string Name, int Arity), bool> automaticClosureIdentities = [];

    private string? ReadFirstSsaClosureName(FunctionDefinition lexicalOwner, Expr.Fun closure, int coreOrdinal) =>
        UsesIndependentSsaClosureIdentity(lexicalOwner, closure, coreOrdinal)
            ? FormattableString.Invariant($"-{lexicalOwner.Name}/{lexicalOwner.Arity}-anonymous-0-")
            : null;

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
            result = CanAssignFirstSsaAnonymousName(signature, automaticClosureSourceFunctions);
            automaticClosureIdentities.Add(signature, result);
        }

        return result;
    }

    private static bool CanAssignFirstSsaAnonymousName(
        (string Name, int Arity) signature,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
    )
    {
        if (!functions.TryGetValue(signature, out var source) || source.Clauses.Count != 1)
        {
            return false;
        }

        // The automatic inliner drops the Core name. An inlined local callee must
        // not introduce another closure, synthetic loop or helper before this one.
        bool found = false;
        var seen = new HashSet<(string Name, int Arity)> { signature };
        var pending = new Queue<FunctionDefinition>();
        pending.Enqueue(source);
        while (pending.TryDequeue(out var function))
        {
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.Fun candidate)
                {
                    if (!ReferenceEquals(function, source) || found || !IsTotalAnonymousClosure(candidate))
                    {
                        return false;
                    }

                    found = true;
                }
                else if (expression is Expr.FunctionReference or Expr.Apply or Expr.Try or Expr.Catch or Expr.Receive
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

    private static bool IsTotalAnonymousClosure(Expr.Fun closure) => closure.SelfName is null
        && closure.Clauses is [{ Guard: null } clause] && HasUnconditionalParameters(clause);
}

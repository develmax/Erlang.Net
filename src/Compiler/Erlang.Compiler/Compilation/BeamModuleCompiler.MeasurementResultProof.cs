namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool ProvesMeasurementReturn(
        (string Name, int Arity) entry,
        bool binary,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
    )
    {
        var active = new HashSet<(string Name, int Arity)>();

        return ProveLocal(entry);

        bool ProveLocal((string Name, int Arity) signature)
        {
            if (IsDeclaredNative(signature) || !functions.TryGetValue(signature, out var target))
            {
                return false;
            }

            // Only terminal edges enter this proof. Returning from a recursive edge
            // requires eventually returning from one of the checked clauses.
            if (!active.Add(signature))
            {
                return true;
            }

            bool result = target.Clauses.All(ProveClause);
            active.Remove(signature);

            return result;
        }

        bool ProveClause(Clause clause) => binary
            && clause.Body is Expr.Variable value
            && clause.Guard is Expr.Call
            {
                Function: AutoImportNames.IsBinary, DynamicRemote: false, Arguments: [Expr.Variable tested]
            } guard
            && value.Name == tested.Name && IsIntrinsic(guard)
            || Prove(clause.Body);

        bool IsIntrinsic(Expr.Call call) =>
            (call.Module ?? (functions.ContainsKey((call.Function, 1))
                ? null : ResolveExternalModule(call.Function, 1))) == ErlangModuleNames.Module;

        bool Prove(Expr expression)
        {
            if (expression is Expr.Call { DynamicRemote: false } call)
            {
                if (call.Arguments.Count == 1)
                {
                    if (call.Function == AutoImportNames.Exit && IsIntrinsic(call))
                    {
                        return true;
                    }

                    if (binary ? call.Function is AutoImportNames.ListToBinary or AutoImportNames.IolistToBinary
                            && IsIntrinsic(call) : call is { Module: "lists", Function: "reverse" })
                    {
                        return true;
                    }
                }

                return call.Module is null && ProveLocal((call.Function, call.Arguments.Count));
            }

            return expression switch
            {
                Expr.Block block => Prove(block.Body),
                Expr.Sequence { Items: [.., var last] } => Prove(last),
                Expr.Case choice => choice.Clauses.All(ProveClause),
                Expr.If choice => choice.Clauses.All(ProveClause),
                Expr.List list => !binary && (list.Tail is null || Prove(list.Tail)),
                Expr.Literal { Value: Nil } => !binary,
                _ => false
            };
        }
    }
}

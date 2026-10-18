namespace Erlang.Compiler;

internal static class SourceBifForwardingContracts
{
    internal static Expr.Call? Resolve(ModuleDefinition module, Expr.Call call, out int indexSlot, out int tupleSlot)
    {
        indexSlot = tupleSlot = -1;
        if (call.Module is not null)
            return null;
        var function = module.Functions.FirstOrDefault(value => value.Name == call.Function && value.Arity == call.Arguments.Count);
        if (function is null || function.Clauses.Count != 1 || function.Clauses[0].Guard is not null)
            return null;
        var clause = function.Clauses[0];
        if (clause.Patterns.Any(pattern => pattern is not Pattern.Variable { Name: not SourceBifReturnParameters.DiscardedVariable }))
            return null;
        var names = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            return null;
        Expr body = clause.Body;
        while (body is Expr.Block || body is Expr.Sequence { Items.Count: 1 })
            body = body is Expr.Block current ? current.Body : ((Expr.Sequence)body).Items[0];
        if (body is not Expr.Call target || target.Function != ErlangModuleNames.Element
            || target.Arguments is not [Expr.Variable index, Expr.Variable tuple]
            || !(target.Module == ErlangModuleNames.Module
                || target.Module is null && SourceCallResolution.IsGuardAutoImport(module, target.Function, target.Arguments.Count)))
            return null;
        indexSlot = Array.IndexOf(names, index.Name);
        tupleSlot = Array.IndexOf(names, tuple.Name);
        if (indexSlot < 0 || tupleSlot < 0
            || call.Arguments[indexSlot] is not Expr.Literal { Value: Integer value }
            || value.Value <= SourceBifReturnParameters.MaximumTupleArity)
            return null;

        return target;
    }
}

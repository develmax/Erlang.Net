namespace Erlang.Compiler;

internal static class SourceBifReturnContracts
{
    internal static bool IsCandidate(ModuleDefinition module, Expr.Call call)
    {
        if (call.Function != ErlangModuleNames.Element
            || call.Arguments.Count != SourceBifReturnParameters.ElementArity
            || call.Arguments[0] is not Expr.Literal { Value: Integer index }
            || index.Value <= SourceBifReturnParameters.MaximumTupleArity)
            return false;

        return call.Module == ErlangModuleNames.Module
            || call.Module is null && SourceCallResolution.IsGuardAutoImport(
                module,
                call.Function,
                call.Arguments.Count
            );
    }
}

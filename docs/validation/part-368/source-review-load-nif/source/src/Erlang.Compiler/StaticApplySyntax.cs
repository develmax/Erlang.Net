namespace Erlang.Compiler;

internal static class StaticApplySyntax
{
    internal static Expr.Call? Lower(Expr.Call call, string? resolvedModule)
    {
        if (resolvedModule != ErlangModuleNames.Module
            || call.Function != ErlangBasicBifNames.Apply
            || call.Arguments.Count != ErlangApplyLayout.ModuleApplyArity
            || call.Arguments[ErlangApplyLayout.ModuleArgument] is not Expr.Literal { Value: Atom module }
            || call.Arguments[ErlangApplyLayout.FunctionArgument] is not Expr.Literal { Value: Atom function }
            || (module.Name == ErlangModuleNames.Module && function.Name == SourceLoadNifNames.Function)
            || call.Arguments[ErlangApplyLayout.ArgumentList] is not Expr.List { Tail: null } arguments
            || arguments.Items.Count > StaticApplyParameters.MaximumDirectArguments)
            return null;

        return new Expr.Call(module.Name, function.Name, arguments.Items) { SourceLine = call.SourceLine };
    }
}

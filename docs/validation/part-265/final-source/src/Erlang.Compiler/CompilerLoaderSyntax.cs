namespace Erlang.Compiler;

internal static class CompilerLoaderSyntax
{
    internal static bool IsStaticApply(Expr.Call call)
        => call.Module is null or ErlangModuleNames.Module &&
            call.Function == ErlangBasicBifNames.Apply &&
            call.Arguments.Count == CompilerLoaderSyntaxArity.ApplyArguments &&
            call.Arguments[CompilerLoaderSyntaxArity.ModuleArgument] is Expr.Literal { Value: Atom { Name: ErlangModuleNames.Module } } &&
            call.Arguments[CompilerLoaderSyntaxArity.FunctionArgument] is Expr.Literal { Value: Atom { Name: CompilerLoaderNames.CallOnLoad } } &&
            call.Arguments[CompilerLoaderSyntaxArity.ArgumentList] is Expr.List { Items.Count: CompilerLoaderSyntaxArity.CallbackArguments, Tail: null };
}

namespace Erlang.Compiler;

internal static class OnLoadDeclarationDiagnostics
{
    internal const string Invalid = "Expected one on_load declaration naming a function of arity zero.";
    internal static string Undefined(string function) => $"The on_load function {function}/0 is undefined.";
}

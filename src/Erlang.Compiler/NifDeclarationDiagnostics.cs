namespace Erlang.Compiler;

internal static class NifDeclarationDiagnostics
{
    internal const string InvalidList = "Nifs attribute requires a proper literal list";
    internal const string InvalidEntry = "Badly formed nifs function attribute";

    internal static string Undefined(string name, int arity) => $"Undefined nif {name}/{arity}";
}

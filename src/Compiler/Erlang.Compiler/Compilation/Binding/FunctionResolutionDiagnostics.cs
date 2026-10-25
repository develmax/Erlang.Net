namespace Erlang.Compiler;

internal static class FunctionResolutionDiagnostics
{
    public static string Undefined(string name, int arity) => $"Undefined function {name}/{arity}";

    public static string UnsupportedNode(string name) => $"Unsupported function-resolution node {name}";
}

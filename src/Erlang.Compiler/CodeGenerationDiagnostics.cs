namespace Erlang.Compiler;

internal static class CodeGenerationDiagnostics
{
    public static string UnsupportedLiteral(string typeName) => "Literal emission is not supported for " + typeName;
}

namespace Erlang.Compiler;

internal static class CodeGenerationDiagnostics
{
    internal static string ParseTransformRequiresBeam(string module) => $"Build-time parse transform requires a selected BEAM module {module}.";

    internal static string ParseTransformDidNotReturn(string module) => $"Build-time parse transform retired without a result for {module}.";

    public static string UnsupportedLiteral(string typeName) => "Literal emission is not supported for " + typeName;
}

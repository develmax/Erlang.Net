using System.Text.Json;

namespace Erlang.Compiler;

internal static class ComprehensionSourceCode
{
    private const string SourceType = "new global::Erlang.Compiler.ComprehensionSource(";
    private const string FunctionsType = "new global::Erlang.Compiler.GeneratedComprehensionFunction[]{";
    private const string FunctionType = "new global::Erlang.Compiler.GeneratedComprehensionFunction(";
    private const string SourceMember = "{Source=";
    private const string DiscardConsMember = "{DiscardConsContinuation=";
    private const string BooleanTrue = "true";
    private const string BooleanFalse = "false";

    internal static string Initializer(ComprehensionSource? source)
    {
        if (source is null)
        {
            return string.Empty;
        }
        string functions = string.Join(
            ',',
            source.Functions.Select(value => FunctionType + value.QualifierIndex
            + "," + JsonSerializer.Serialize(value.Function) + "," + value.Arity + "," + value.CoreOrdinal + "," + value.LiftedOrdinal + "," + value.SourceLine + ")" + DiscardConsMember
            + (value.DiscardConsContinuation ? BooleanTrue : BooleanFalse) + "}")
        );

        return SourceMember + SourceType + JsonSerializer.Serialize(source.Owner) + "," + source.OwnerArity
            + "," + source.Line + "," + source.Offset + "," + FunctionsType + functions + "}," + (source.TerminalBody ? BooleanTrue : BooleanFalse) + ")}";
    }
}

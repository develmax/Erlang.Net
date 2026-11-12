namespace Erlang.Compiler;

internal static class BeamMacroDiagnostics
{
    internal const string InvalidDefinition = "Expected a valid object-like or parameterized macro declaration.";
    internal const string InvalidArguments = "Invalid macro argument list.";
    internal const string UnsupportedCondition = "Unsupported preprocessing conditional expression.";
    internal const string UnsupportedFunctionContext = "Function context macros require a complete function head.";
    internal const string IncludeLimit = "BEAM source preprocessing exceeded the include depth or size limit.";
    internal const string UnsupportedStringification = "Macro stringification does not support this token.";
    internal const string FeatureAfterPrefix = "The feature directive must appear in the source prefix.";
    internal static string UnsupportedFeature(string name) => $"Unsupported source feature '{name}'.";
    internal static string DocumentationMissing(string path) => $"Could not resolve documentation file '{path}'.";
    internal static string IncludeMissing(string path) => $"Could not resolve include '{path}'.";
    internal static string IncludedError(string path, string message) => $"In include '{path}': {message}";
    internal static string ArgumentMismatch(string name) => $"No macro '{name}' matches the argument count.";
    internal const string ExpectedName = "Expected a macro name after '?'.";
    internal const string ExpansionLimit = "BEAM source preprocessing exceeded the macro expansion limit.";
    internal const string UnclosedConditional = "Unclosed preprocessing conditional.";
    internal static string InvalidDirective(string name) => $"Invalid preprocessing directive '-{name}'.";
    internal static string UnsupportedDirective(string name) => $"Unsupported preprocessing directive '-{name}'.";
    internal static string UnexpectedConditional(string name) => $"Unexpected preprocessing directive '-{name}'.";
    internal static string Redefined(string name) => $"Macro '{name}' is already defined.";
    internal static string Undefined(string name) => $"Macro '{name}' is undefined or requires an unsupported preprocessing directive.";
}

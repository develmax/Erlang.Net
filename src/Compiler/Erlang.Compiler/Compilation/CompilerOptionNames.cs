namespace Erlang.Compiler;

internal static class CompilerOptionNames
{
    internal const string NoAutoImportTypes = "no_auto_import_types";
    internal const string SuppressUnsafeFunctionWarning = "nowarn_possibly_unsafe_function";
    public const string DisableSsaOptimization = "no_ssa_opt";
    internal const string Inline = "inline";
    internal const string NoInline = "no_inline";
    internal const string InlineSize = "inline_size";
}

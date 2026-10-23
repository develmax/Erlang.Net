namespace Erlang.Compiler;

internal static class SourceCallBindingDiagnostics
{
    public const string InvalidDirective = "Invalid function import or no_auto_import directive";

    public static string DuplicateImport(string name, int arity) => $"Function {name}/{arity} already imported";

    public static string ImportLocal(string name, int arity) => $"Imported function {name}/{arity} is locally defined";

    public static string AutoImportClash(string name, int arity) => $"Ambiguous call of overridden auto-imported function {name}/{arity}";

    public static string ImportedFun(string name, int arity) => $"Creating a fun from imported name {name}/{arity} is not allowed";
}

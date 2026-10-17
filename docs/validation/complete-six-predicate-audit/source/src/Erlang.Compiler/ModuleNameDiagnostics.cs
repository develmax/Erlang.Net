namespace Erlang.Compiler;

internal static class ModuleNameDiagnostics
{
    public const string NonLatin1 = "module names with non-latin1 characters are not supported";
    public const string Empty = "the module name must not be empty";
    public const string Blank = "the module name must contain at least one visible character";
    public const string Control = "the module name must not contain control characters";
}

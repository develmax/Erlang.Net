namespace Erlang;

internal static class NifNativeLoadReasonText
{
    internal const string DisplayTerminator = "Term display writer did not terminate its display record.";
    internal const string ModulePrefix = "Library module name '";
    internal const string CallingModulePrefix = "' does not match calling module '";
    internal const string CallingModuleSuffix = "'";
    internal const string MissingFunctionPrefix = "Function not found ";
    internal const string UndeclaredFunctionPrefix = "Function not declared as nif ";
    internal const string DuplicateFunctionPrefix = "Duplicate NIF entry for ";
    internal const string ModuleFunctionSeparator = ":";
    internal const string FunctionAritySeparator = "/";
    internal const string VetoPrefix = "Library load-call unsuccessful (";
    internal const string VetoSuffix = ").";
}

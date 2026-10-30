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
    internal const string OldCodePrefix544 = "Calling load_nif from old module '";
    internal const string OldCodeSuffix544 = "' not allowed";
    internal const string Reload544 = "NIF library already loaded (reload disallowed since OTP 20).";
    internal const string UpgradeUnsupported544 = "Upgrade not supported by this NIF library.";
}

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyNegativeNames
{
    internal const string Functions = "functions";
    internal const string ExecutionIdentity = "ExecutionIdentity";
    internal const string NativePrimitives = "NativePrimitives";
    internal const string MissingFunction = "missing_library393_mfa";
    internal const int OriginalMathPrimitiveCount = 24;
    internal const string StringOwnerFailure = "Selected library ownership changed during startup: string";
    internal const string RuntimeOwnerFailure = "Selected library ownership changed during startup: erlang";
    internal const string PureOwnerFailure = "Selected library ownership changed during startup: library393_pure";
    internal const string StringPreparationFailure = "Startup library selection failed: string";
    internal const string StringPrimitiveFailure = "Required native primitive is unavailable: string:list_to_float/1";
    internal const string StringMissingExportFailure = "Selected BEAM image omits a managed library export: string:list_to_float/1";
    internal const string MissingSelectedMfaFailure = "Pure dependency must be explicitly selected with the required MFA: library393_pure -> erlang:missing_library393_mfa/1";
}

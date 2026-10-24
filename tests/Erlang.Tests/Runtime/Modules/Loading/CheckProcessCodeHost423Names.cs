namespace Erlang.Tests;

internal static class CheckProcessCodeHost423Names
{
    internal const string BindingTest = "runtime/internal-check-own-code423-binding-owner-cancel";
    internal const string OwnershipTest = "runtime/internal-check-own-code423-real-old-source-continuation";
    internal const string ExitTest = "runtime/internal-check-own-code423-exit-releases-real-source-ownership";
    internal const string Failure = "Own process old-code query differs from actual source ownership";
    internal const string Module = "erts_internal";
    internal const string Function = "check_process_code";
    internal const string Subject = "check_code_host423";
    internal const string Callback = "check_code_host_callback423";
    internal const string Hold = "hold";
    internal const string Run = "run";
    internal const string Missing = "no_such_check_code_module423";
    internal const string True = "true";
    internal const string False = "false";
    internal const string First = "v1";
    internal const string Normal = "normal";
    internal const string Unwound = "Unwound";
    internal const string Kill = "kill";
    internal const string NativeEntry = "IsNativeEntry";
}

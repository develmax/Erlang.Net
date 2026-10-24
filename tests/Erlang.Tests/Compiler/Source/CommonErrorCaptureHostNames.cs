namespace Erlang.Tests;

internal static class CommonErrorCaptureHostNames
{
    internal const string Fault = "host_capture_fault";
    internal const string Erlang = "erlang";
    internal const string Head = "hd";
    internal const string Rollback = "intentional_rollback";
    internal const string RollbackMissing = "rollback was not exercised";
    internal const string CallerModule = "host_capture_probe";
    internal const string CallerFunction = "caller";
    internal const string TemporaryModule = "host_body_only";
    internal const string TemporaryFunction = "temporary";
    internal const string SystemFlag = "system_flag";
    internal const string Flag = "backtrace_depth";
    internal const string SuppliedModule = "supplied";
    internal const string SuppliedFunction = "trace";
    internal const string ExplicitFault = "explicit_fault";
    internal const string PriorHostFault = "prior_host_fault";
    internal const string Error = "error";
    internal const string BareFault = "bare_fault";
    internal const string ErpcType = "Erlang.Otp.Erpc";
    internal const string ErpcStack = "Stack";
    internal const string ActualTarget = "actual_target";
    internal const string ActualFunction = "actual_function";
    internal const string ExpectedFailure = "expected Erlang exception";
    internal const string ContractFailure = "common exception capture host contract failed";
}

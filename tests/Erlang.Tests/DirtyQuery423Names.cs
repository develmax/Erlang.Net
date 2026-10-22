namespace Erlang.Tests;

internal static class DirtyQuery423Names
{
    internal const string CpuTest = "runtime/dirty-query423/actual-cpu-running-queued-and-settled";
    internal const string IoTest = "runtime/dirty-query423/actual-io-running-queued-and-settled";
    internal const string BindingTest = "runtime/dirty-query423/native-owner-binding-and-live-dead-pid";
    internal const string Module = "erts_internal", Function = "is_process_executing_dirty", True = "true", False = "false";
    internal const string Failure = "Dirty execution query423 source protocol failed";
    internal const string NativeEntry = "IsNativeEntry";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}

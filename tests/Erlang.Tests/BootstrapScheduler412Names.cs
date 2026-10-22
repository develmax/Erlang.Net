namespace Erlang.Tests;

internal static class BootstrapScheduler412Names
{
    internal const string BindingTest = "runtime/bootstrap-scheduler412/optional-authentic-binding-and-refusal";
    internal const string MeasurementTest = "runtime/bootstrap-scheduler412/actual-two-lane-wall-time-reset-and-disable";
    internal const string ValuesTest = "runtime/bootstrap-scheduler412/any-nontrue-term-disables-and-cancelled-caller";
    internal const string Module = "erts_internal", Function = "scheduler_wall_time";
    internal const string Erlang = "erlang", ProcessFlag = "process_flag", Scheduler = "scheduler";
    internal const string True = "true", False = "false", Other = "scheduler412_nontrue";
    internal const string Failure = "Managed scheduler wall time or bootstrap native binding differs";
    internal const int Capacity = 2, WorkMilliseconds = 20, SpinQuantum = 256;
}

namespace Erlang;

internal static class PollVmNames1570
{
    internal const string All = "all";
    internal const string Eager = "eager-two-vms";
    internal const string Isolation = "dispose-isolation";
    internal const string Halt = "halt-no-flush";
    internal const string InitDeath = "initializer-no-host-dispose";
    internal const string BeforeStart = "fail-before-start";
    internal const string BeforeAccounting = "fail-before-accounting";
    internal const string Entered = "fail-entered-before-ready";
    internal const string Contract = "Actual per-VM Linux poll lifetime contract failed";
    internal const string Sentinel = "genuine-poll-zero-creation-sentinel";
    internal const string Poll = "poll";
    internal const string Thread = "thread";
    internal const string Lifetime = "descriptorPollingLifetime558";
    internal const string RuntimeGate = "gate";
    internal const string AccountingRecords = "records";
    internal const string WakeRead = "wakeRead";
    internal const string WakeWrite = "wakeWrite";
    internal const string ProcTasks = "/proc/self/task/";
    internal const int NormalWorkers = 1;
    internal const int DirtyIoWorkers = 1;
    internal const int PollIdentity = 0;
    internal const int LiveValue = 71;
    internal const int HaltStatus = 17;
    internal const int ClosedWakeFd = -1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal static readonly string[] Cases = [Eager, Isolation, Halt, InitDeath, BeforeStart, BeforeAccounting, Entered];
}

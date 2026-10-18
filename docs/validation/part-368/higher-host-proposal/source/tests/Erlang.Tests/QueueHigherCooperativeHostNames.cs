namespace Erlang.Tests;

internal static class QueueHigherCooperativeHostNames
{
    internal const string Test = "runtime/queue/higher/later-phase-cooperative-cancellation-cleanup";
    internal const string Failure = "Queue higher traversal failed its real continuation, phase, result or physical unwind contract.";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Normal = "normal";
    internal const int CallbackArity = 1;
    internal const int FirstRearMatchCallbacks = 1;
    internal const int Cells = 80017;
    internal const int SplitPoint = Cells / 2;
    internal const int MinimumObservedReductions = 1024;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}

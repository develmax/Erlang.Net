namespace Erlang;

internal static class ProcessSchedulerNames
{
    internal const string Binding = "scheduler";
    internal const string Identity = "scheduler_id";
    internal const string Online = "schedulers_online";
    internal const string Active = "schedulers_active";
    internal const string State = "schedulers_state";
    internal const string Total = "schedulers_total";
    internal const string OnlineTurnMismatch = "Scheduler online request lost its admitted turn.";
    internal const int Unbound = 0;
    internal const int First = 1;
}

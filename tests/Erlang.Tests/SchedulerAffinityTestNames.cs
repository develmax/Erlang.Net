namespace Erlang.Tests;

internal static class SchedulerAffinityTestNames
{
    internal const string CompiledPrefix = "oracle/compiled-module/";
    internal const string First = "scheduler/first-instruction-and-migration";
    internal const string Park = "scheduler/offline-child-park-resume";
    internal const string Init = "scheduler/initializer-mailbox-readmission-reentry";
    internal const string Retire = "scheduler/initializer-retirement-after-guests";
    internal const string Invalid = "scheduler/invalid-values-preserve-state";
    internal const string Public = "scheduler/all-local-public-spawn-option-and-request-forms";
    internal const string Granted = "scheduler/parked-granted-head-pending-kill-administrative-retirement";
    internal const string Active = "scheduler/active-cancel-retains-quiescence";
    internal const string Queued = "scheduler/canceled-queued-waiter-successor";
    internal const string GuestKill = "scheduler/offline-guest-kill-admitted-after-reonline";
    internal const string Offline = "scheduler/offline-pending-guest-kill-distinct-retirement";
}

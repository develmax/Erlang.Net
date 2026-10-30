namespace Erlang.Tests;

internal static class StatisticsTrapTestNames
{
    internal const string Authentic = "runtime/statistics-trap/authentic-private-and-preloaded-image";
    internal const string Replies = "runtime/statistics-trap/actual-callback-replies-and-exact-ref-cleanup";
    internal const string Fault = "runtime/statistics-trap/callback-fault-interrupts-real-receive";
    internal const string Cancel = "runtime/statistics-trap/real-requester-kill-drains-callbacks";
    internal const string Close = "runtime/statistics-trap/close-waits-for-helper-and-callbacks";
    internal const string Failure = "Genuine statistics helper admission, receive or physical protocol differs";
    internal const string CallbackFailure = "actual-normal-callback-before-statistics-reply";
    internal const string IndependentFailure = "independent-held-normal-callback-failure";
    internal const string Module = "erts_internal";
    internal const string Helper = "await_sched_wall_time_modifications";
    internal const string Resource = "Erlang.Runtime.PinnedOtp.erts_internal.beam";
    internal const string ImageHash = "6740F088697BCF6285227B95D3B91ECFF38AE89ADF9A1B6976413CEC2AF7B806";
    internal const string RequestType = "Erlang.StatisticsTrapRequest";
    internal const string ForeignMessage = "statistics_trap_foreign_message";
    internal static readonly IReadOnlyList<string> ImageDependencies = Array.AsReadOnly<string>(
        ["erlang", "error_logger", "erts_debug", "erts_internal", "erts_literal_area_collector", "ets", "kernel_refc", "persistent_term"]);
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
}

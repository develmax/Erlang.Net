namespace Erlang;

public static class SystemMonitorNames
{
    public const string Function = "system_monitor";
    public const string Undefined = "undefined";
    public const string Notification = "monitor";
    public const string LongQueue = "long_message_queue";
    public const string LongGarbageCollection = "long_gc";
    public const string LongSchedule = "long_schedule";
    public const string LargeHeap = "large_heap";
    public const string BusyPort = "busy_port";
    public const string BusyDistributionPort = "busy_dist_port";
    public const string Unsupported = "not_supported";
    public const string UnsupportedOption = "system_monitor_option";
    public const string ErrorModule = "module";
    public const string ErrorFormatter = "erl_erts_errors";

    public static ErlangException UnsupportedEffect(string option) => new(Term.Tuple(
        Term.A(Unsupported),
        Term.Tuple(Term.A(UnsupportedOption), Term.A(option))
    ));
}

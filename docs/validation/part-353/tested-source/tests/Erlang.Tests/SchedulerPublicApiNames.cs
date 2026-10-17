namespace Erlang.Tests;

internal static class SchedulerPublicApiNames
{
    internal const string Schedulers = "schedulers";
    internal const string Total = "schedulers_total";
    internal const string Module = "scheduler_public_host";
    internal const string Child = "child";
    internal const string SpawnRequest = "spawn_request";
    internal const string Reply = "spawn_reply";
    internal const string Success = "ok";
    internal const int DownArity = 5;
    internal const int DownReasonIndex = 4;
    internal const int ReferenceIndex = 1;
    internal const int ReplyArity = 4;
    internal const int ReplyStatusIndex = 2;
    internal const string BadArgument = "badarg";
    internal const string InvalidAtom = "wrong_scheduler";
    internal const int InvalidNegative = -1;
    internal const int InvalidAboveConfigured = 3;
    internal const int BigIntegerShift = 100;
}

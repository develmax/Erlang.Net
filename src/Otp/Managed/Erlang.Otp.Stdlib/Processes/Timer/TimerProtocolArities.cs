namespace Erlang;

internal static class TimerProtocolArities
{
    internal const int Reference = 2;
    internal const int Destination = 2;
    internal const int Timestamp = 3;
    internal const int Mfa = 3;
    internal const int Result = 2;
    internal const int ResultWithInfo = 3;
    internal const int ServerRequest = 2;
    internal const int OnceRequest = 3;
    internal const int IntervalRequest = 4;
    internal const int TimeoutMessage = 3;
    internal const int DownMessage = 5;
    internal const int CancelMessage = 2;
    internal const int IntervalAction = 4;
    internal const int OnceEntry = 1;
    internal const int IntervalEntry = 3;
    internal const int SendArguments = 2;
    internal const int SpawnAction = 2;
}

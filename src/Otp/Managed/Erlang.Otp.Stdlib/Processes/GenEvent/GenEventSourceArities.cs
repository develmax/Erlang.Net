namespace Erlang;

internal static class GenEventSourceArities
{
    internal const int Send = 2;
    internal const int Call = 3;
    internal const int CallWithTimeout = 4;
    internal const int Rpc = 2;
    internal const int HandleMsg = 6;
    internal const int ServerAddHandler = 4;
    internal const int ServerDeleteHandler = 4;
    internal const int ServerCall = 4;
    internal const int ServerCallUpdate = 3;
    internal const int ServerUpdate = 4;
    internal const int ServerNotify = 4;
    internal const int DoTerminate = 7;
    internal const int ReportError = 5;
    internal const int SystemCodeChange = 4;
    internal const int CodeChangeCallback = 4;
    internal const int FilterMap = 2;
}

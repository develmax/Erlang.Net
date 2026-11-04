namespace Erlang;

internal static class GenEventMessageLayout
{
    internal const int NotifySize = 2;
    internal const int ExitSize = 3;
    internal const int SystemSize = 3;
    internal const int RequestSize = 3;
    internal const int TagIndex = 0;
    internal const int NotifyEventIndex = 1;
    internal const int ExitSenderIndex = 1;
    internal const int ExitReasonIndex = 2;
    internal const int SystemFromIndex = 1;
    internal const int SystemRequestIndex = 2;
    internal const int RequestFromIndex = 0;
    internal const int RequestReplyTagIndex = 1;
    internal const int RequestCommandIndex = 2;
}

namespace Erlang;

internal static class GenEventRequestLayout
{
    internal const int OperationIndex = 0;
    internal const int EventIndex = 1;
    internal const int QueryIndex = 2;
    internal const int HandlerIndex = 1;
    internal const int ArgumentsIndex = 2;
    internal const int SupervisorIndex = 3;
    internal const int SwapOldHandlerIndex = 1;
    internal const int SwapOldArgumentsIndex = 2;
    internal const int SwapNewHandlerIndex = 3;
    internal const int SwapNewArgumentsIndex = 4;
    internal const int SwapSupervisorIndex = 5;
    internal const int SyncNotifySize = 2;
    internal const int CallSize = 3;
    internal const int AddSize = 3;
    internal const int AddSupervisedSize = 4;
    internal const int DeleteSize = 3;
    internal const int SwapSize = 5;
    internal const int SwapSupervisedSize = 6;
}

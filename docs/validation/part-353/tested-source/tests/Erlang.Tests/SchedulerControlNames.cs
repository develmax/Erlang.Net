namespace Erlang.Tests;

internal static class SchedulerControlNames
{
    internal const string Exit = "exit";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Shutdown = "shutdown";
    internal const string Dispatcher = "actorDispatcher";
    internal const string Gate = "gate";
    internal const string OnlineCount = "OnlineCount";
    internal const string OnlineChange = "onlineChange";
    internal const string OnlineTurn = "onlineTurn";
    internal const string Requests = "onlineRequests";
    internal const string Owner = "Owner";
    internal const string Count = "Count";
    internal const string ActorScheduler = "ActorTaskScheduler";
    internal const string ReadyNode = "ReadyNode";
    internal const string Completion = "Completion";
    internal const string TurnType = "Erlang.RuntimeActorExecutionTurn";
    internal const string LaneIdentity = "LaneIdentity";
    internal const int PollMilliseconds = 1;
    internal const int OneQueuedRequest = 1;
    internal const int TwoQueuedRequests = 2;
    internal const int NoRequests = 0;
}

using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class SchedulerControlInspection
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static object Dispatcher(ProcessRuntime runtime) => typeof(ProcessRuntime)
        .GetField(SchedulerControlNames.Dispatcher, Instance)!.GetValue(runtime)!;

    internal static SchedulerControlSnapshot Snapshot(ProcessRuntime runtime)
    {
        object dispatcher = Dispatcher(runtime);
        Type type = dispatcher.GetType();
        object gate = type.GetField(SchedulerControlNames.Gate, Instance)!.GetValue(dispatcher)!;
        lock (gate)
        {
            object requests = type.GetField(SchedulerControlNames.Requests, Instance)!.GetValue(dispatcher)!;

            return new(
                (int)type.GetProperty(SchedulerControlNames.OnlineCount, Instance)!.GetValue(dispatcher)!,
                Owner(type.GetField(SchedulerControlNames.OnlineChange, Instance)!.GetValue(dispatcher)),
                Owner(type.GetField(SchedulerControlNames.OnlineTurn, Instance)!.GetValue(dispatcher)),
                (int)requests.GetType().GetProperty(SchedulerControlNames.Count, Instance)!.GetValue(requests)!
            );
        }
    }

    internal static bool Ready(ProcessContext context)
    {
        object dispatcher = Dispatcher(context.Runtime);
        object gate = dispatcher.GetType().GetField(SchedulerControlNames.Gate, Instance)!.GetValue(dispatcher)!;
        lock (gate)
        {
            object scheduler = typeof(ProcessContext).GetProperty(SchedulerControlNames.ActorScheduler, Instance)!.GetValue(context)!;

            return scheduler.GetType().GetProperty(SchedulerControlNames.ReadyNode, Instance)!.GetValue(scheduler) is not null;
        }
    }

    internal static int ActualLane() => (int)typeof(ProcessRuntime).Assembly.GetType(SchedulerControlNames.TurnType, true)!
        .GetProperty(SchedulerControlNames.LaneIdentity, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    internal static Task Completion(ProcessRuntime runtime)
    {
        object dispatcher = Dispatcher(runtime);

        return (Task)dispatcher.GetType().GetProperty(SchedulerControlNames.Completion, Instance)!.GetValue(dispatcher)!;
    }

    internal static async Task Wait(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > SchedulerControlCalls.Deadline)
                throw new TimeoutException(SchedulerAffinityHostNames.Failure);
            await Task.Delay(SchedulerControlNames.PollMilliseconds);
        }
    }

    private static Pid? Owner(object? request) => request is null ? null
        : ((ProcessContext)request.GetType().GetProperty(SchedulerControlNames.Owner, Instance)!.GetValue(request)!).Self;
}

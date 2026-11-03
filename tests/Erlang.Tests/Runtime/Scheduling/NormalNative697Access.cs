using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class NormalNative697Access
{
    internal static IDisposable Enter(ProcessContext context)
        => (IDisposable)Call(context.Runtime, NormalNative697Names.Enter, context)!;

    internal static async Task<IDisposable> Barrier(ProcessContext context)
    {
        object pending = Call(context.Runtime, NormalNative697Names.Acquire, context)!;
        var task = (Task)Call(pending, NormalNative697Names.AsTask)!;
        await task;

        return (IDisposable)task.GetType().GetProperty(NormalNative697Names.Result)!.GetValue(task)!;
    }

    internal static object? Call(object target, string method, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, NormalNative697Names.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static (int Active, int Queued) Counts(ProcessRuntime runtime)
    {
        object dispatcher = Dispatcher(runtime);
        object gate = dispatcher.GetType().GetField(NormalNative697Names.Gate, NormalNative697Names.Members)!.GetValue(dispatcher)!;
        lock (gate)
        {
            int active = (int)dispatcher.GetType().GetField(NormalNative697Names.Active, NormalNative697Names.Members)!.GetValue(dispatcher)!;
            object queue = dispatcher.GetType().GetField(NormalNative697Names.Queue, NormalNative697Names.Members)!.GetValue(dispatcher)!;
            int count = (int)queue.GetType().GetProperty(NormalNative697Names.Count)!.GetValue(queue)!;

            return (active, count);
        }
    }

    internal static async Task WaitCounts(ProcessRuntime runtime, int active, int queued)
    {
        using var timeout = new CancellationTokenSource(NormalNative697Names.Deadline);
        while (Counts(runtime) != (active, queued)) await Task.Delay(1, timeout.Token);
    }

    internal static Task Auxiliary(ProcessRuntime runtime, Action<int> body)
    {
        object batch = Call(Dispatcher(runtime), NormalNative697Names.PrepareAuxiliary, body)!;
        Call(batch, NormalNative697Names.Publish);

        return (Task)batch.GetType().GetProperty(NormalNative697Names.Completion, NormalNative697Names.Members)!.GetValue(batch)!;
    }

    internal static Task Close(ProcessRuntime runtime)
        => (Task)Call(runtime.Modules, NormalNative697Names.Close)!;

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NormalNative697Names.Failure);
    }

    private static object Dispatcher(ProcessRuntime runtime)
        => runtime.GetType().GetField(NormalNative697Names.Dispatcher, NormalNative697Names.Members)!.GetValue(runtime)!;
}

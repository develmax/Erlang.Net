using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class StatisticsTrapTestAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(StatisticsTrapTestNames.Failure);
    }

    internal static object? Read(object target, string name)
        => target.GetType().GetField(name, Members)?.GetValue(target)
            ?? target.GetType().GetProperty(name, Members)?.GetValue(target);

    internal static object? Call(object target, string name, params object?[] values)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, values);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object Request(ProcessRuntime runtime, ProcessContext caller, ReferenceTerm reference)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(StatisticsTrapTestNames.RequestType, true)!;

        return Activator.CreateInstance(
            type,
            Members,
            null,
            [runtime, caller, reference],
            null
        )!;
    }

    internal static async Task<object> Admission(ProcessContext context)
    {
        object result = Call(
            context.Runtime.Modules,
            "AdmitStatisticsTrap",
            context,
            StatisticsTrapTestNames.Helper,
            2
        )!;
        Task task = (Task)result.GetType().GetMethod("AsTask")!.Invoke(result, null)!;
        await task;

        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    internal static async Task WaitUntil(Func<bool> predicate)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (!predicate())
        {
            Require(System.Diagnostics.Stopwatch.GetElapsedTime(started) < StatisticsTrapTestNames.Deadline);
            await Task.Delay(1);
        }
    }

    internal static async Task<Exception> Failed(Task task, string? marker = null)
    {
        Exception? failure = null;
        try
        {
            await task.WaitAsync(StatisticsTrapTestNames.Deadline);
        }
        catch (Exception error) when (error is not TimeoutException)
        {
            failure = error;
        }
        Require(failure is not null && (marker is null || failure.ToString().Contains(marker, StringComparison.Ordinal)));

        return failure!;
    }
}

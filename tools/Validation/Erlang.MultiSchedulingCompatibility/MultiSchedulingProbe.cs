using Erlang;
using static MultiSchedulingProbeNames;

internal static class MultiSchedulingProbe
{
    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static ValueTask<Term> FlagCall(ProcessContext context, string operation)
        => context.Runtime.Modules.Call(
            context,
            Runtime,
            Flag,
            Term.A(Multi),
            Term.A(operation)
        );

    internal static ValueTask<Term> InfoCall(ProcessContext context, string key)
        => context.Runtime.Modules.Call(
            context,
            Runtime,
            Info,
            Term.A(key)
        );

    internal static (ProcessHandle Handle, Task<Term> Result) Start(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle handle = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await body(context);
                result.TrySetResult(value);

                return value;
            }
            catch (Exception error)
            {
                result.TrySetException(error);
                throw;
            }
        });

        return (handle, result.Task);
    }

    internal static async Task<Term> Finish((ProcessHandle Handle, Task<Term> Result) worker)
    {
        Term result = await worker.Result.WaitAsync(Deadline);
        await worker.Handle.Completion.WaitAsync(Deadline);

        return result;
    }

    internal static async ValueTask<Term> Dirty(
        ProcessContext context,
        string kind,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body,
        TaskCompletionSource submitted
    )
    {
        object selectedKind = Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(Kind, true)!, kind);
        var pending = (ValueTask<Term>)typeof(ProcessRuntime).GetMethod(Schedule, Members)!.Invoke(
            context.Runtime,
            [context, selectedKind, Runtime, Schedule, Array.Empty<Term>(), body, null])!;
        submitted.TrySetResult();

        return await pending;
    }

    internal static object? Owner(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(DirtyOwner, Members)!.GetValue(runtime);

    internal static bool IsPaused(ProcessRuntime runtime)
    {
        object? owner = Owner(runtime);
        if (owner is null)
            return false;
        lock (owner.GetType().GetField(Gate, Members)!.GetValue(owner)!)
        {
            return (bool)owner.GetType().GetField(Paused, Members)!.GetValue(owner)!;
        }
    }

    internal static T ReadSnapshot<T>(ProcessRuntime runtime, string property)
    {
        object owner = Owner(runtime)!;
        object value = owner.GetType().GetMethod(Snapshot, Members)!.Invoke(owner, null)!;

        return (T)value.GetType().GetProperty(property)!.GetValue(value)!;
    }

    internal static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(Deadline);
        while (!condition())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    internal static bool BarrierPending(ProcessRuntime runtime)
    {
        object dispatcher = typeof(ProcessRuntime).GetField(Dispatcher, Members)!.GetValue(runtime)!;
        lock (dispatcher.GetType().GetField(Gate, Members)!.GetValue(dispatcher)!)
        {
            object? barrier = dispatcher.GetType().GetField(BarrierField, Members)!.GetValue(dispatcher);

            return barrier is not null && !(bool)barrier.GetType().GetProperty(Granted, Members)!.GetValue(barrier)!;
        }
    }

    internal static async Task KillProcess(ProcessRuntime runtime, Pid target)
    {
        await Finish(Start(runtime, context =>
        {
            runtime.Exit(context, target, Term.A(Kill));

            return ValueTask.FromResult<Term>(Nil.Value);
        }));
    }
}

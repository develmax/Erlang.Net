using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class DriverIndependentTurn455Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(3);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        using var owner = (IDisposable)typeof(ProcessRuntime).GetMethod(DriverIndependentTurn455Names.Acquire, flags)!.Invoke(runtime, null)!;
        MethodInfo dispatch = typeof(ProcessRuntime).GetMethod(DriverIndependentTurn455Names.Dispatch, flags)!;
        MethodInfo require = typeof(ProcessRuntime).Assembly.GetType(DriverIndependentTurn455Names.Permit)!
            .GetMethod(DriverIndependentTurn455Names.Require, BindingFlags.Static | BindingFlags.NonPublic)!;

        Task Queue(Func<Task> body) => (Task)dispatch.Invoke(runtime, [owner, body])!;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task held = Queue(() =>
        {
            require.Invoke(null, [runtime]);
            entered.TrySetResult();

            return release.Task;
        });
        Task pending = Task.CompletedTask;
        try
        {
            await entered.Task.WaitAsync(DriverIndependentTurn455Names.Deadline);
            // Cross the round-robin boundary while the first real lane stays
            // occupied. The third independent turn used to queue behind it.
            for (int index = 0; index < 3; index++)
            {
                pending = Queue(() =>
                {
                    require.Invoke(null, [runtime]);

                    return Task.CompletedTask;
                });
                await pending.WaitAsync(DriverIndependentTurn455Names.Deadline);
            }
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(held, pending).WaitAsync(DriverIndependentTurn455Names.Deadline);
        }
    }
}

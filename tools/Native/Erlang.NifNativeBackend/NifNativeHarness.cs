namespace Erlang.NifNativeBackend;

internal static class NifNativeHarness
{
    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }

    internal static async Task Context(ProcessRuntime runtime, Func<ProcessContext, Task> operation)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            try
            {
                await operation(context);
                completed.TrySetResult();
            }
            catch (Exception failure)
            {
                completed.TrySetException(failure);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(NifNativeHarnessNames.Deadline);
        await process.Completion.WaitAsync(NifNativeHarnessNames.Deadline);
    }

    internal static void Guest(Action operation, Term expected)
    {
        try
        {
            operation();
        }
        catch (ErlangException failure) when (failure.Reason.Equals(expected))
        {
            return;
        }
        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }
}

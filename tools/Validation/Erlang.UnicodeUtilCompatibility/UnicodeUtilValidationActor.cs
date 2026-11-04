using System.Threading.Channels;
using Erlang;

internal sealed class UnicodeUtilValidationActor : IAsyncDisposable
{
    private readonly Channel<Func<ProcessContext, Task>> commands = Channel.CreateUnbounded<Func<ProcessContext, Task>>();
    private readonly ProcessHandle worker;
    private readonly ProcessRuntime runtime;

    internal UnicodeUtilValidationActor(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        worker = runtime.Spawn(async context =>
        {
            await foreach (var command in commands.Reader.ReadAllAsync(context.Cancellation)) await command(context);

            return Nil.Value;
        });
    }

    internal async Task<T> InvokeAsync<T>(Func<ProcessContext, Task<T>> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        await commands.Writer.WriteAsync(async context =>
        {
            try
            {
                completion.TrySetResult(await action(context));
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        });

        try
        {
            return await completion.Task.WaitAsync(UnicodeUtilValidationNames.Deadline);
        }
        catch (TimeoutException)
        {
            // A failed command must settle its owned actor before another run.
            await runtime.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        commands.Writer.TryComplete();
        await worker.Completion.WaitAsync(UnicodeUtilValidationNames.Deadline);
    }
}

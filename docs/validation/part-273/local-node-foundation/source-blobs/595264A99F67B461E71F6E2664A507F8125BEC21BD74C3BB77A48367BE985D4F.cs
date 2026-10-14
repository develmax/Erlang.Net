using Erlang;
using System.Threading.Channels;

namespace Erlang.Tests;

internal sealed class RuntimeNodeFoundationHostActor : IAsyncDisposable
{
    private readonly Channel<Func<ProcessContext, Task>> commands = Channel.CreateUnbounded<Func<ProcessContext, Task>>();
    private readonly TaskCompletionSource<ProcessContext> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal RuntimeNodeFoundationHostActor(ProcessRuntime runtime)
    {
        Process = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            try
            {
                await foreach (var command in commands.Reader.ReadAllAsync(context.Cancellation))
                    await command(context);
            }
            finally
            {
                commands.Writer.TryComplete();
            }

            return Nil.Value;
        });
    }

    internal ProcessHandle Process { get; }
    internal Task<ProcessContext> Ready => ready.Task;

    internal Task<T> Invoke<T>(Func<ProcessContext, T> command)
        => InvokeAsync(context => Task.FromResult(command(context)));

    internal async Task<T> InvokeAsync<T>(Func<ProcessContext, Task<T>> command)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        await commands.Writer.WriteAsync(async context =>
        {
            try
            {
                result.TrySetResult(await command(context));
            }
            catch (Exception exception)
            {
                result.TrySetException(exception);
            }
        });

        return await result.Task.WaitAsync(Names.Deadline);
    }

    public async ValueTask DisposeAsync()
    {
        commands.Writer.TryComplete();
        await Process.Completion.WaitAsync(Names.Deadline);
    }
}

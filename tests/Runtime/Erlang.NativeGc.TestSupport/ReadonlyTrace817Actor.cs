using System.Threading.Channels;

namespace Erlang.NifNativeBackend;

internal sealed class ReadonlyTrace817Actor : IAsyncDisposable
{
    private readonly Channel<Func<ProcessContext, Task>> commands = Channel.CreateUnbounded<Func<ProcessContext, Task>>();
    private readonly TaskCompletionSource<ProcessContext> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task<ProcessContext> Ready => ready.Task;
    private readonly ProcessHandle process;

    internal ReadonlyTrace817Actor(ProcessRuntime runtime, ProcessMessageQueueDataMode411 mode)
    {
        process = runtime.Spawn(async context =>
        {
            context.Mailbox.ExchangeMessageQueueData411(mode);
            ready.TrySetResult(context);
            await foreach (Func<ProcessContext, Task> command in commands.Reader.ReadAllAsync(context.Cancellation))
                await command(context);

            return Nil.Value;
        });
    }

    internal async Task InvokeAsync(Func<ProcessContext, Task> body)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await commands.Writer.WriteAsync(async context =>
        {
            try
            {
                // Reenter the real actor scheduler without draining the pending
                // alias before the synchronous event scope is installed.
                await context.ResumeExecution();
                await body(context);
                done.TrySetResult();
            }
            catch (Exception error)
            {
                done.TrySetException(error);
            }
        });
        await done.Task.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
    }

    public async ValueTask DisposeAsync()
    {
        commands.Writer.TryComplete();
        Term reason = await process.Completion.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(ReadonlyTrace817CaseNames.Failure);
    }
}

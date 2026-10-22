using Erlang;
namespace Erlang.Tests;

internal sealed class InternalProcess425HostTarget : IAsyncDisposable
{
    private readonly ProcessRuntime runtime;
    private readonly TaskCompletionSource<ProcessContext> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ProcessHandle process;

    internal InternalProcess425HostTarget(ProcessRuntime owner)
    {
        runtime = owner;
        process = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            await context.ReceiveAsync<Term>(term => term.Equals(Term.A(InternalProcess425HostNames.Stop)) ? term : null);

            return Nil.Value;
        });
    }

    internal Task<ProcessContext> Ready => ready.Task;

    public async ValueTask DisposeAsync()
    {
        runtime.Send(process.Pid, Term.A(InternalProcess425HostNames.Stop));
        await process.Completion.WaitAsync(InternalProcess425HostNames.Deadline);
    }
}

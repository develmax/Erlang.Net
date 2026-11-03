using System.Threading.Channels;

namespace Erlang.NifNativeBackend;

internal sealed class SchedulerWallTimeTestActor : IAsyncDisposable
{
    private readonly Channel<Func<ProcessContext, Task>> commands = Channel.CreateUnbounded<Func<ProcessContext, Task>>();
    internal ProcessHandle Process { get; }

    internal SchedulerWallTimeTestActor(ProcessRuntime runtime)
    {
        Process = runtime.Spawn(async context =>
        {
            await foreach (var command in commands.Reader.ReadAllAsync(context.Cancellation))
                await command(context);

            return Nil.Value;
        });
    }

    internal async Task<Term> Call(string function, params Term[] arguments)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        await commands.Writer.WriteAsync(async context =>
        {
            try
            {
                result.TrySetResult(await context.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    function,
                    arguments
                ));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }
        });

        return await result.Task.WaitAsync(CpuStatisticsParameters.Deadline);
    }

    internal Task<Term> Flag(bool enabled) => Call(SequenceTraceNames.SystemFlag,
        Term.A(RuntimeStatisticsNames.SchedulerWallTime), CoreModules.Boolean(enabled));

    internal Task<Term> Sample(bool all = false) => Call(
        RuntimeStatisticsNames.Function,
        Term.A(all ? RuntimeStatisticsNames.SchedulerWallTimeAll : RuntimeStatisticsNames.SchedulerWallTime)
    );

    public async ValueTask DisposeAsync()
    {
        commands.Writer.TryComplete();
        Term reason = await Process.Completion.WaitAsync(CpuStatisticsParameters.Deadline);
        CpuStatisticsAccess.Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
    }
}

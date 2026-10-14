namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Task CallbackExecutionUnwind(Pid callback)
    {
        lock (gate)
            return executingProcesses.FirstOrDefault(process => process.Self.Equals(callback))?.Unwound.Task ?? Task.CompletedTask;
    }
}

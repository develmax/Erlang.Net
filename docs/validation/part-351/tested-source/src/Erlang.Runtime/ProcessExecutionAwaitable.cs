namespace Erlang;

public readonly struct ProcessExecutionAwaitable
{
    private readonly ValueTask pending;
    private readonly RuntimeProcessTaskScheduler? scheduler;
    private readonly ProcessExecutionAdmission admission;

    internal ProcessExecutionAwaitable(ValueTask pending, RuntimeProcessTaskScheduler? scheduler, ProcessExecutionAdmission admission)
    {
        this.pending = pending;
        this.scheduler = scheduler;
        this.admission = admission;
    }

    public ProcessExecutionAwaiter GetAwaiter() => new(pending, scheduler, admission);
}

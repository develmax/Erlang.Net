namespace Erlang;

public sealed class ProcessContext : ITermExecutionContext
{
    internal ProcessContext(ProcessRuntime runtime, Pid pid)
    {
        Runtime = runtime;
        Self = pid;
    }

    internal CancellationTokenSource Stop { get; } = new();
    internal TaskCompletionSource<Term> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal HashSet<Pid> Links { get; } = new();
    internal bool Terminated;
    private int reductions;
    public ProcessRuntime Runtime
    {
        get;
    }
    public Pid Self
    {
        get;
    }
    public Mailbox Mailbox { get; } = new();
    public bool TrapExits
    {
        get; set;
    }
    public Term? ExitReason
    {
        get; internal set;
    }
    public CancellationToken Cancellation => Stop.Token;
    public Dictionary<Term, Term> Dictionary { get; } = new();

    public async ValueTask ReduceAsync()
    {
        Cancellation.ThrowIfCancellationRequested();
        if (++reductions >= 2000)
        {
            reductions = 0;
            await Task.Yield();
            Cancellation.ThrowIfCancellationRequested();
        }
    }

    public ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout = null) where T : class => Mailbox.ReceiveAsync(select, timeout, Cancellation);

    public void Exit(Term reason) => throw new ErlangException(reason, ErlangExceptionClasses.Exit);
}

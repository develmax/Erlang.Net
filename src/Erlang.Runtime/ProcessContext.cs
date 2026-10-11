namespace Erlang;

public sealed class ProcessContext : ITermExecutionContext
{
    internal ProcessContext(ProcessRuntime runtime, Pid pid)
    {
        Runtime = runtime;
        Self = pid;
        GroupLeader = pid;
        Mailbox = new() { ProcessSignals = () => Runtime.AdmitSignals(this) };
    }

    internal CancellationTokenSource Stop { get; } = new();
    internal Pid GroupLeader { get; set; }
    internal TaskCompletionSource<Term> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal HashSet<Pid> Links { get; } = new();
    internal bool Terminated;
    private int reductions;
    private readonly AsyncLocal<ErlangExecutionFrame?> executionFrame = new();

    private sealed class ExecutionFrameScope(ProcessContext context, ErlangExecutionFrame? parent) : IDisposable
    {
        public void Dispose() => context.executionFrame.Value = parent;
    }

    public string? CurrentFunction => executionFrame.Value?.Function;
    public int CurrentFunctionArity => executionFrame.Value?.Arity ?? 0;

    public IDisposable EnterFunction(string module, string function, int arity)
    {
        var previous = executionFrame.Value;
        executionFrame.Value = new(
            module,
            function,
            arity,
            previous
        );

        return new ExecutionFrameScope(this, previous);
    }

    internal Term CaptureErrorStack(Term arguments, MapTerm? info, string? builtin = null)
    {
        var frames = new List<Term>();
        Term location = info is null ? Nil.Value : Term.List(Term.Tuple(Term.A(ErlangErrorMetadataNames.ErrorInfo), info));
        var frame = executionFrame.Value;
        if (builtin is not null)
            frames.Add(Term.Tuple(
                Term.A(ErlangModuleNames.Module),
                Term.A(builtin),
                arguments,
                location
            ));
        bool first = builtin is null;
        int depth = Math.Max(1, Runtime.BacktraceDepth);
        while (frame is not null && frames.Count < depth)
        {
            frames.Add(Term.Tuple(
                Term.A(frame.Module),
                Term.A(frame.Function),
                first && arguments is Cons or Nil ? arguments : Term.I(frame.Arity),
                first ? location : Nil.Value
            ));
            first = false;
            frame = frame.Parent;
        }

        return Cons.From(frames);
    }

    public ProcessRuntime Runtime
    {
        get;
    }
    public Pid Self
    {
        get;
    }
    public Mailbox Mailbox { get; }
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

    public ValueTask ReduceAsync() => ChargeAsync(1);

    internal async ValueTask ChargeAsync(int count)
    {
        Cancellation.ThrowIfCancellationRequested();
        reductions += count;
        if (reductions >= ProcessSchedulingLimits.CooperativeSlice)
        {
            reductions = 0;
            Runtime.AdmitSignals(this);
            await Task.Yield();
            Cancellation.ThrowIfCancellationRequested();
        }
    }

    public ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout = null) where T : class => Mailbox.ReceiveAsync(select, timeout, Cancellation);

    public void Exit(Term reason) => throw new ErlangException(reason, ErlangExceptionClasses.Exit);
}

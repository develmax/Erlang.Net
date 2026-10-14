namespace Erlang;

public sealed class ProcessContext : ITermExecutionContext
{
    internal ProcessContext(ProcessRuntime runtime, Pid pid)
    {
        Runtime = runtime;
        Self = pid;
        GroupLeader = pid;
        Mailbox = new()
        {
            ProcessSignals = () => Runtime.AdmitSignals(this),
            WaitForExecution = WaitIfSuspended,
            ExecutionBlocked = () => SuspensionRequested,
            MessageReceived = SavedCalls.Received,
            MessageAdmitted = (message, from) => Runtime.ProcessTracing.Admitted(this, message, from),
            TraceReceived = (message, token, preserve) => Runtime.SequenceTracing.Received(
                this,
                message,
                token,
                preserve
            ),
            ReceiveTimedOut = () =>
            {
                SavedCalls.TimedOut();
                Runtime.ProcessTracing.Admitted(this, Term.A(ProcessTraceNames.Timeout), Term.A(ProcessTraceNames.ClockService));
            },
            WaitingStateChanged = waiting => WaitingForMessage = waiting,
            QueueLengthChanged = count => Runtime.SystemMonitoring.QueueChanged(Self, count)
        };
    }

    internal CancellationTokenSource Stop { get; } = new();
    internal Pid GroupLeader { get; set; }
    internal TaskCompletionSource<Term> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Unwound { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal HashSet<Pid> Links { get; } = new();
    internal Dictionary<Pid, ProcessLinkIdentity> LinkIdentities { get; } = new();
    internal Dictionary<ProcessLinkIdentity, Pid> PendingLinkOrigins { get; } = new();
    internal HashSet<PortTerm> PortLinks { get; } = new();
    internal HashSet<PortTerm> PriorityPortLinks { get; } = new();
    internal HashSet<Pid> PriorityLinks { get; } = new();
    internal bool Terminated;
    internal volatile bool SuspensionRequested;
    internal HashSet<ProcessCodeRequest> PendingCodeChecks { get; } = new();
    internal HashSet<ProcessGarbageCollectionRequest> PendingGarbageCollections { get; } = new();
    internal HashSet<ProcessInfoRequest> PendingInfoRequests { get; } = new();
    internal HashSet<ProcessDisplayRequest> PendingDisplayRequests { get; } = new();
    internal volatile bool WaitingForMessage;
    internal volatile bool ExecutionStarted;
    internal volatile bool Yielding;
    internal HashSet<ProcessSavedCallRequest> PendingSavedCallRequests { get; } = new();
    internal ProcessSavedCalls SavedCalls { get; } = new();
    internal SequenceTraceState SequentialTrace { get; } = new();
    private int reductions;
    private readonly AsyncLocal<ErlangExecutionFrame?> executionFrame = new();
    private ErlangExecutionFrame? visibleExecutionFrame;
    private readonly object codeGate = new();
    private readonly Dictionary<ModuleCodeImage, int> activeCode = new();
    private readonly AsyncLocal<SourceExecution?> sourceExecution = new();

    private sealed record SourceExecution(ModuleCodeImage Image, object Owner, SourceExecution? Parent);

    private sealed class SourceExecutionScope(ProcessContext context, SourceExecution current) : IDisposable
    {
        private int disposed;

        public void RestoreFlow() => context.sourceExecution.Value = current.Parent;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            RestoreFlow();
        }
    }

    private sealed class SourceCodeScope(SourceCodeLease lease, IDisposable flow) : IDisposable
    {
        public void RestoreFlow() => ((SourceExecutionScope)flow).RestoreFlow();

        public void Dispose()
        {
            flow.Dispose();
            lease.Dispose();
        }
    }

    public ModuleCodeImage? ExecutingSourceImage(object owner)
    {
        var current = sourceExecution.Value;

        return current is not null && ReferenceEquals(current.Owner, owner) ? current.Image : null;
    }

    public IDisposable EnterSourceCode(ModuleCodeImage image, object owner)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(owner);
        var lease = Runtime.Modules.ReserveSourceCode(this, image);

        return new SourceCodeScope(lease, lease.Attach(owner));
    }

    internal SourceCodeLease ReserveSourceCode(ModuleCodeImage image)
    {
        lock (codeGate)
            activeCode[image] = activeCode.GetValueOrDefault(image) + 1;

        return new SourceCodeLease(this, image);
    }

    internal IDisposable AttachSourceCode(ModuleCodeImage image, object owner)
    {
        var current = new SourceExecution(image, owner, sourceExecution.Value);
        sourceExecution.Value = current;

        return new SourceExecutionScope(this, current);
    }

    internal void ReleaseSourceCode(ModuleCodeImage image)
    {
        lock (codeGate)
        {
            int count = activeCode[image];
            if (count == 1)
                activeCode.Remove(image);
            else
                activeCode[image] = count - 1;
        }
    }

    internal bool IsExecutingCode(ModuleCodeImage image)
    {
        lock (codeGate)
            return activeCode.ContainsKey(image);
    }

    internal void RestoreSourceCodeFlow(IDisposable scope) => ((SourceCodeScope)scope).RestoreFlow();

    private sealed class ExecutionFrameScope(ProcessContext context, ErlangExecutionFrame? parent) : IDisposable
    {
        public void Dispose()
        {
            context.executionFrame.Value = parent;
            Volatile.Write(ref context.visibleExecutionFrame, parent);
        }
    }

    public string? CurrentFunction => executionFrame.Value?.Function;
    public int CurrentFunctionArity => executionFrame.Value?.Arity ?? 0;

    internal Term SnapshotCurrentFunction() => ProcessInfoFrames.Current(Volatile.Read(ref visibleExecutionFrame));

    internal Term SnapshotCurrentStackTrace() => ProcessInfoFrames.Stack(Volatile.Read(ref visibleExecutionFrame), Runtime.BacktraceDepth);

    internal IReadOnlyList<ErlangExecutionFrame> SnapshotDisplayFrames()
    {
        var frames = new List<ErlangExecutionFrame>();
        ErlangExecutionFrame? frame = Volatile.Read(ref visibleExecutionFrame);
        while (frame is not null)
        {
            frames.Add(frame);
            frame = frame.Parent;
        }

        return frames;
    }

    public IDisposable EnterFunction(string module, string function, int arity)
    {
        var previous = executionFrame.Value;
        executionFrame.Value = new(
            module,
            function,
            arity,
            previous
        );
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

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
        if (SuspensionRequested)
            await WaitIfSuspended();
        reductions += count;
        if (reductions >= ProcessSchedulingLimits.CooperativeSlice)
        {
            reductions = 0;
            Runtime.AdmitSignals(this);
            Yielding = true;
            try
            {
                await Task.Yield();
            }
            finally
            {
                Yielding = false;
            }
            Cancellation.ThrowIfCancellationRequested();
        }
    }

    internal async ValueTask<long> WaitIfSuspended()
    {
        if (!SuspensionRequested)
            return 0;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        while (SuspensionRequested)
        {
            Cancellation.ThrowIfCancellationRequested();
            Runtime.AdmitSignals(this);
            Task? resumed = Runtime.Suspensions.EnterPause(this);
            if (resumed is null)
                break;
            await Mailbox.WaitForControlCompletion(resumed, Cancellation);
        }

        return System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    public ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout = null) where T : class => Mailbox.ReceiveAsync(select, timeout, Cancellation);

    public void Exit(Term reason) => throw new ErlangException(reason, ErlangExceptionClasses.Exit);
}

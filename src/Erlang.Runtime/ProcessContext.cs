namespace Erlang;

public sealed partial class ProcessContext : ITermExecutionContext
{
    internal ProcessContext(
        ProcessRuntime runtime,
        Pid pid,
        Term? initialCall = null
    )
    {
        Runtime = runtime;
        InitialCall = initialCall ?? ProcessInfoAtoms.Undefined;
        Self = pid;
        GroupLeader = pid;
        Mailbox = new()
        {
            IncomingTerm = Runtime.ObserveIncomingTerm,
            HaltRequested = () => Runtime.IsHaltRequested,
            ProcessSignals = () => Runtime.AdmitSignals(this),
            WaitForExecution = WaitIfSuspended,
            SuspensionTime = () => Interlocked.Read(ref cumulativeSuspensionTicks),
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

    internal volatile DistributionSignalDrain? DeferredSignalDrain;
    internal CancellationTokenSource Stop { get; } = new();
    internal Pid GroupLeader
    {
        get; set;
    }
    internal TaskCompletionSource<Term> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Unwound { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal HashSet<Pid> Links { get; } = new();
    internal Dictionary<Pid, ProcessLinkIdentity> LinkIdentities { get; } = new();
    internal Dictionary<ProcessLinkIdentity, Pid> PendingLinkOrigins { get; } = new();
    internal HashSet<PortTerm> PortLinks { get; } = new();
    internal HashSet<PortTerm> PriorityPortLinks { get; } = new();
    internal HashSet<Pid> PriorityLinks { get; } = new();
    internal bool AsyncDistribution
    {
        get; set;
    }
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
    private long cumulativeReductions;
    internal Term InitialCall
    {
        get;
    }
    private Atom errorHandler = ProcessInfoLogicalAtoms.DefaultErrorHandler;
    internal Atom ErrorHandler
    {
        get => Volatile.Read(ref errorHandler);
        set => Volatile.Write(ref errorHandler, value);
    }
    private int schedulingPriority = (int)ProcessSchedulingPriority.Normal;
    internal RuntimeProcessTaskScheduler? ActorTaskScheduler { get; set; }
    internal int SchedulerBinding { get; set; }
    internal ProcessSchedulingPriority SchedulingPriorityLevel
    {
        get => (ProcessSchedulingPriority)Volatile.Read(ref schedulingPriority);
        set => Volatile.Write(ref schedulingPriority, (int)value);
    }
    internal Atom SchedulingPriority => ProcessPriorityValues.ToAtom(SchedulingPriorityLevel);
    internal long CumulativeReductions => Interlocked.Read(ref cumulativeReductions);
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

    internal SourceCodeEntry? CaptureExecutingCode()
    {
        var caller = sourceExecution.Value;

        return caller is not null && IsExecutingCode(caller.Image) ? new SourceCodeEntry(caller.Owner, caller.Image) : null;
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
        => EnterFrame(
            module,
            function,
            arity,
            false
        );

    internal IDisposable EnterSourceWrapper(
        string module,
        string function,
        int arity
    )
        => EnterFrame(
            module,
            function,
            arity,
            true
        );

    internal IDisposable EnterSourceWrapperBody(
        string module,
        string function,
        int arity
    )
    {
        var previous = executionFrame.Value;
        var parent = previous is { SourceWrapper: true } && previous.Module == module ? previous.Parent : previous;
        executionFrame.Value = new(
            module,
            function,
            arity,
            parent,
            true
        );
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    private IDisposable EnterFrame(
        string module,
        string function,
        int arity,
        bool sourceWrapper
    )
    {
        var previous = executionFrame.Value;
        executionFrame.Value = new(
            module,
            function,
            arity,
            previous,
            sourceWrapper
        );
        var selected = selectedErrorHandler.Value;
        if (!sourceWrapper && selected is not null && selected.Frame is null
            && selected.Module == module && selected.Function == function && selected.Arguments.Count == arity)
            selected.Frame = executionFrame.Value;
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal ErlangExecutionFrame? CaptureExecutionChain() => executionFrame.Value;

    internal ErlangException CaptureSourceFailure(
        ErlangException exception,
        IReadOnlyList<Term>? arguments = null
    ) => RuntimeExceptionCapture.Source(
        this,
        exception,
        executionFrame.Value,
        arguments
    );

    internal ErlangException CaptureCallFailure(
        ErlangException exception,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    ) => CaptureUntrustedCallFailure(
        exception,
        module,
        function,
        arguments
    );

    internal ErlangException CaptureNativeFailure(
        ErlangException exception,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    ) => RuntimeExceptionCapture.Call(
        this,
        exception,
        executionFrame.Value,
        module,
        function,
        arguments,
        true
    );

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
        if (builtin is not null && frame is { SourceWrapper: true }
            && frame.Module == ErlangModuleNames.Module && frame.Function == builtin
            && arguments is Cons or Nil && Cons.Items(arguments).Count() == frame.Arity)
            frame = frame.Parent;
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
    internal RuntimeDistributionRole DistributionRole
    {
        get; set;
    }
    public Mailbox Mailbox
    {
        get;
    }
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

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public void Halt(int status) => Runtime.Halt(this, status);

    public ValueTask ReduceAsync() => ChargeAsync(1);

    public ProcessExecutionAwaitable ExecutionCheckpoint() => new(
        ChargeAsync(1), ActorTaskScheduler, ProcessExecutionAdmission.Continuation);

    public ProcessExecutionAwaitable YieldExecution() => new(
        ValueTask.CompletedTask, ActorTaskScheduler, ProcessExecutionAdmission.StackUnwind);

    private ProcessExecutionAwaitable YieldReduction() => new(
        ValueTask.CompletedTask, ActorTaskScheduler, ProcessExecutionAdmission.Reduction);

    internal ValueTask ChargeAsync(int count) => ChargeAsync(count, capToRemainingSlice: false);

    internal ValueTask BumpReductionsAsync(int count) => ChargeAsync(count, capToRemainingSlice: true);

    private async ValueTask ChargeAsync(int count, bool capToRemainingSlice)
    {
        Runtime.ThrowIfHalted();
        Cancellation.ThrowIfCancellationRequested();
        if (SuspensionRequested)
            await WaitIfSuspended();
        if (capToRemainingSlice)
            count = Math.Min(count, ProcessSchedulingLimits.CooperativeSlice - reductions);
        Interlocked.Add(ref cumulativeReductions, count);
        Runtime.Statistics.RecordReductions(count);
        reductions += count;
        if (reductions >= ProcessSchedulingLimits.CooperativeSlice)
        {
            reductions = 0;
            Runtime.AdmitSignals(this);
            Yielding = true;
            try
            {
                await YieldReduction();
            }
            finally
            {
                Yielding = false;
            }
            Runtime.ThrowIfHalted();
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
            Runtime.ThrowIfHalted();
            Cancellation.ThrowIfCancellationRequested();
            Runtime.AdmitSignals(this);
            Task? resumed = Runtime.Suspensions.EnterPause(this);
            if (resumed is null)
                break;
            await Mailbox.WaitForControlCompletion(resumed, Cancellation);
        }

        long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        Interlocked.Add(ref cumulativeSuspensionTicks, elapsed);

        return elapsed;
    }

    public ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout = null) where T : class
        => ReceiveWithCancellation(select, timeout, Cancellation);

    internal ValueTask<T?> ReceiveErlangAsync<T>(Func<Term, T?> select, Term timeout) where T : class
        => ReceiveWithCancellation(
            select,
            null,
            Cancellation,
            timeout
        );

    internal async ValueTask<T?> ReceiveForOperationAsync<T>(
        Func<Term, T?> select,
        CancellationToken operationCancellation
    ) where T : class
    {
        if (!operationCancellation.CanBeCanceled || operationCancellation == Cancellation)
            return await ReceiveWithCancellation(select, null, Cancellation);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(Cancellation, operationCancellation);

        return await ReceiveWithCancellation(select, null, linked.Token);
    }

    private async ValueTask<T?> ReceiveWithCancellation<T>(
        Func<Term, T?> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Term? erlangTimeout = null
    ) where T : class
    {
        Runtime.ThrowIfHalted();
        try
        {
            T? result = erlangTimeout is null
                ? await Mailbox.ReceiveAsync(select, timeout, cancellation)
                : await Mailbox.ReceiveErlangAsync(select, erlangTimeout, cancellation);
            Runtime.ThrowIfHalted();

            return result;
        }
        catch (OperationCanceledException) when (Runtime.IsHaltRequested)
        {
            throw new ProcessHaltTransfer(Runtime);
        }
    }

    public void Exit(Term reason) => throw new ErlangException(reason, ErlangExceptionClasses.Exit);
}

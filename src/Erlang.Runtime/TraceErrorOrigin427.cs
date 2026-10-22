namespace Erlang;

internal sealed class TraceErrorOrigin427 : IDisposable
{
    private static readonly AsyncLocal<TraceErrorOrigin427?> current = new();
    private TraceErrorOrigin427? previous;
    private ProcessContext? context;
    private IReadOnlyList<Term>? arguments;
    private object? frame;
    private readonly string module;
    private readonly string function;
    private int disposed;

    private TraceErrorOrigin427(
        ProcessContext caller,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        context = caller;
        this.module = module;
        this.function = function;
        this.arguments = arguments;
        frame = caller.CaptureTraceErrorFrame427();
        previous = current.Value;
        current.Value = this;
    }

    internal static TraceErrorOrigin427 Enter(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
        => new(
            context,
            module,
            function,
            arguments
        );

    internal static bool TryCapture(ProcessContext caller, MapTerm info, out ErlangException error)
    {
        TraceErrorOrigin427? origin = current.Value;
        ProcessContext? originContext = origin?.context;
        IReadOnlyList<Term>? originalArguments = origin?.arguments;
        object? originFrame = origin?.frame;
        if (origin is null || Volatile.Read(ref origin.disposed) != 0 || originalArguments is null || !ReferenceEquals(originContext, caller)
            || !caller.IsTraceErrorFrame427(originFrame))
        {
            error = null!;

            return false;
        }
        // The exception has no stack yet: capture the real entry origin exactly
        // once here, rather than parsing or replacing any completed frame.
        error = caller.CaptureNativeFailureWithInfo(
            new ErlangException(ErlangErrorReasons.BadArgument),
            origin.module,
            origin.function,
            originalArguments,
            info
        );

        return true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        if (ReferenceEquals(current.Value, this)) current.Value = previous;
        // A captured child ExecutionContext may retain a closed scope object,
        // but it must neither reuse its origin nor retain its guest arguments.
        previous = null;
        context = null;
        arguments = null;
        frame = null;
    }
}

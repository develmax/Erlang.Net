namespace Erlang;

internal sealed class InternalTraceBinding425
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal InternalTraceBinding425(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!InternalTraceNative425.TryCapture(
            runtime,
            InternalTraceNames425.Module,
            function,
            arity,
            out kernel!
        ))
            throw new InvalidOperationException(InternalTraceNames425.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

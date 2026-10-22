namespace Erlang;

internal sealed class InternalScalarBinding413
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal InternalScalarBinding413(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!ErtsInternalScalarNative413.TryCapture(
            runtime,
            ErtsInternalScalarNativeNames413.Module,
            function,
            arity,
            out kernel!
        ))
            throw new InvalidOperationException(ErtsInternalScalarNativeNames413.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

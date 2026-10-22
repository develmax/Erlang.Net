namespace Erlang;

internal sealed class DebugDirtyBinding424
{
    private readonly ErlangFunction kernel;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal DebugDirtyBinding424(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!DebugDirtyNative424.TryCapture(
            runtime,
            DebugDirtyNames424.Module,
            function,
            arity,
            out kernel!
        ))
            throw new InvalidOperationException(DebugDirtyNames424.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

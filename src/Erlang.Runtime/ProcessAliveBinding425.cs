namespace Erlang;

internal sealed class ProcessAliveBinding425
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal ProcessAliveBinding425(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!ProcessAliveNative425.TryCapture(
            runtime,
            ProcessAliveNames425.Module,
            ProcessAliveNames425.Function,
            ProcessAliveNames425.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(ProcessAliveNames425.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

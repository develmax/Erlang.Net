namespace Erlang;

internal sealed class DirtyQueryBinding423
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal DirtyQueryBinding423(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!DirtyQueryNative423.TryCapture(
            runtime,
            DirtyQueryNames423.Module,
            DirtyQueryNames423.Function,
            DirtyQueryNames423.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(DirtyQueryNames423.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

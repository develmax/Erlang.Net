namespace Erlang;

internal sealed class BeamfileMd5Binding423
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal BeamfileMd5Binding423(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!BeamfileMd5Native423.TryCapture(
            runtime,
            BeamfileMd5Names423.Module,
            BeamfileMd5Names423.Function,
            BeamfileMd5Names423.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(BeamfileMd5Names423.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

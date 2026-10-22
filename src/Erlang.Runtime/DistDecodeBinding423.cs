namespace Erlang;

internal sealed class DistDecodeBinding423
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal DistDecodeBinding423(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!DistDecodeNative423.TryCapture(
            runtime,
            DistDecodeNames423.Module,
            DistDecodeNames423.Function,
            DistDecodeNames423.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(DistDecodeNames423.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

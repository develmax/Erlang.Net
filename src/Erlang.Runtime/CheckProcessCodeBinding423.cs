namespace Erlang;

internal sealed class CheckProcessCodeBinding423
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal CheckProcessCodeBinding423(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!CheckProcessCodeNative423.TryCapture(
            runtime,
            CheckProcessCodeNames423.Module,
            CheckProcessCodeNames423.Function,
            CheckProcessCodeNames423.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(CheckProcessCodeNames423.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

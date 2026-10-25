namespace Erlang;

internal sealed class DistChannel429Binding
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal DistChannel429Binding(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!DistChannel429Native.TryCapture(
            runtime,
            DistChannel429Names.Module,
            DistChannel429Names.Function,
            DistChannel429Names.Arity,
            out kernel!
        )) throw new InvalidOperationException(DistChannel429Names.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
        => kernel(context, arguments);
}

namespace Erlang;

internal sealed class InternalDistributionProfile429Binding
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal InternalDistributionProfile429Binding(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!InternalDistributionProfile429Native.TryCapture(
            runtime,
            InternalDistributionProfile429Names.Module,
            InternalDistributionProfile429Names.Function,
            InternalDistributionProfile429Names.Arity,
            out kernel!
        )) throw new InvalidOperationException(InternalDistributionProfile429Names.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
        => kernel(context, arguments);
}

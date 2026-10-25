namespace Erlang;

internal sealed class DistSpawn430Binding
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal DistSpawn430Binding(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!DistSpawn430Native.TryCapture(
            runtime,
            DistSpawn430Names.Module,
            DistSpawn430Names.Function,
            DistSpawn430Names.Arity,
            out kernel!
        )) throw new InvalidOperationException(DistSpawn430Names.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
        => kernel(context, arguments);
}

namespace Erlang;

internal sealed class MapKeysBinding426
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal MapKeysBinding426(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!MapKeysNative426.TryCapture(
            runtime,
            MapKeysNames426.Module,
            MapKeysNames426.Function,
            MapKeysNames426.Arity,
            out kernel!
        ))
            throw new InvalidOperationException(MapKeysNames426.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

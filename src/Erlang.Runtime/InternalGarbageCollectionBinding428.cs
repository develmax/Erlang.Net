namespace Erlang;

internal sealed class InternalGarbageCollectionBinding428
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal InternalGarbageCollectionBinding428(ProcessRuntime runtime)
    {
        Runtime = runtime;
        if (!InternalGarbageCollectionNative428.TryCapture(
            runtime,
            InternalGarbageCollectionNames428.Module,
            InternalGarbageCollectionNames428.Function,
            InternalGarbageCollectionNames428.Arity,
            out kernel!
        )) throw new InvalidOperationException(InternalGarbageCollectionNames428.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
        => kernel(context, arguments);
}

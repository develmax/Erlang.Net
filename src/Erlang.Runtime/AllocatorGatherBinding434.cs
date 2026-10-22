namespace Erlang;

internal sealed class AllocatorGatherBinding434
{
    internal AllocatorGatherBinding434(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!AllocatorGatherNative434.TryCapture(
            runtime,
            AllocatorGatherNames434.Module,
            function,
            arity,
            out ErlangFunction body
        ))
            throw new ArgumentException(AllocatorGatherNames434.Arity);
        Invoke = body;
        Body = Call;
    }

    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Invoke { get; }
    internal ErlangFunction Body { get; }

    private ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments) => Invoke(context, arguments);
}

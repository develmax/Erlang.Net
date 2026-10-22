namespace Erlang;

internal sealed class HamtMapBinding430
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal HamtMapBinding430(
        ProcessRuntime runtime,
        string module,
        string function,
        int arity
    )
    {
        Runtime = runtime;
        if (!HamtMapNative430.TryCapture(
            runtime,
            module,
            function,
            arity,
            out kernel!
        ))
            throw new ArgumentException(HamtNativeNames430.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

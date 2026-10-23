namespace Erlang;

internal sealed class DebugDispatchBinding432
{
    internal DebugDispatchBinding432(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!DebugDispatchNative432.TryCapture(
            runtime,
            DebugDispatchNames432.Module,
            function,
            arity,
            out ErlangFunction body
        ))
            throw new ArgumentException(DebugDispatchNames432.Arity);
        Invoke = body;
        Body = Call;
    }

    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Invoke { get; }
    internal ErlangFunction Body { get; }

    private ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments) => Invoke(context, arguments);
}

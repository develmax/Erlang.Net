namespace Erlang;

internal sealed class DebugCodeBinding427
{
    internal DebugCodeBinding427(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!DebugCodeNative427.TryCapture(
            runtime,
            DebugCodeNames427.Module,
            function,
            arity,
            out ErlangFunction invoke
        ))
            throw new NotSupportedException(DebugCodeNames427.Profile);
        Invoke = invoke;
        Body = Call;
    }

    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Invoke { get; }
    internal ErlangFunction Body { get; }

    private ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments) => Invoke(context, arguments);
}

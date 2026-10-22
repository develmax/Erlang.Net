namespace Erlang;

internal sealed class DebugSharingBinding425
{
    internal DebugSharingBinding425(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        if (!DebugSharingNative425.TryCapture(
            runtime,
            DebugSharingNames425.Module,
            function,
            arity,
            out ErlangFunction body
        ))
            throw new ArgumentException(DebugSharingNames425.Arity);
        Invoke = body;
        Body = Call;
    }

    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Invoke { get; }
    internal ErlangFunction Body { get; }

    private ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments) => Invoke(context, arguments);
}

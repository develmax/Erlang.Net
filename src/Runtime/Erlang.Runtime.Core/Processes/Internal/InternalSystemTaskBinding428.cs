namespace Erlang;

internal sealed class InternalSystemTaskBinding428
{
    internal ProcessRuntime Runtime { get; }
    internal string Function { get; }
    internal int Arity { get; }
    internal ErlangFunction Body { get; }
    private readonly ErlangFunction kernel;

    internal InternalSystemTaskBinding428(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        Function = function;
        Arity = arity;
        if (!InternalSystemTaskNative428.TryCapture(
            runtime,
            SystemTaskNames428.Module,
            function,
            arity,
            out kernel!
        ))
            throw new InvalidOperationException(SystemTaskNames428.Foreign);
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments) => kernel(context, arguments);
}

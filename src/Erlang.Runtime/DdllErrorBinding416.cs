namespace Erlang;

internal sealed class DdllErrorBinding416
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal DdllErrorBinding416(ProcessRuntime runtime)
    {
        Runtime = runtime;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Runtime.RequireDdllErrorActor416(context);
        if (arguments.Count != 1) throw new ArgumentException(DdllErrorNames416.Foreign);
        try
        {
            return ValueTask.FromResult(DdllErrorWindows416.Format(arguments[0]));
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                DdllErrorNames416.ModuleName,
                DdllErrorNames416.Function,
                arguments
            );
        }
    }
}

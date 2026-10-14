namespace Erlang;

public sealed class ProcessHaltTransfer : Exception
{
    public ProcessRuntime Runtime { get; }

    internal ProcessHaltTransfer(ProcessRuntime runtime)
    {
        Runtime = runtime;
    }
}

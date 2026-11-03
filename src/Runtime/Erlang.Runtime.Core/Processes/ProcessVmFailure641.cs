namespace Erlang;

internal sealed class ProcessVmFailure641(ProcessContext context, RuntimeProcessPayload641 origin) : Exception
{
    internal ProcessRuntime Runtime641 { get; } = context.Runtime;
    internal Pid Process641 { get; } = context.Self;
    internal RuntimeProcessPayload641 Origin641 { get; } = origin;
    internal Exception? Teardown641 { get; set; }
}

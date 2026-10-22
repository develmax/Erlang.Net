namespace Erlang;

internal sealed class RuntimeEtsHeir419(Pid pid, WeakReference<ProcessContext> generation, RuntimeEtsStoredObject? data)
{
    internal Pid Pid { get; } = pid;
    internal WeakReference<ProcessContext> Generation { get; } = generation;
    // null is native THE_NON_VALUE: ownership passes without transfer message.
    internal RuntimeEtsStoredObject? Data { get; } = data;
}

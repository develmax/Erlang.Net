namespace Erlang;

internal sealed class ErlDriverPortJob420
{
    internal ErlDriverPortJob420(
        ulong identity,
        ErlDriverPortIdentity420 port,
        ErlDriverPortTaskKind420 kind,
        Func<bool, bool> invoke,
        Action? drop
    )
    {
        Identity = identity;
        Port = port;
        Kind = kind;
        Invoke = invoke;
        Drop = drop;
    }

    internal ulong Identity { get; }
    internal ErlDriverPortIdentity420 Port { get; }
    internal ErlDriverPortTaskKind420 Kind { get; }
    internal Func<bool, bool> Invoke { get; }
    internal Action? Drop { get; }
    internal TaskCompletionSource Start { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource InvocationDone { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Invoked;
    internal bool Canceled;
    internal bool Dropped;
}

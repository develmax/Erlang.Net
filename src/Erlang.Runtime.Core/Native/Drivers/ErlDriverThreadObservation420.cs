namespace Erlang;

internal sealed class ErlDriverThreadObservation420(ulong id, ErlDriverImageLease420 lease)
{
    internal ulong Id { get; } = id;
    internal ErlDriverImageLease420 Lease { get; } = lease;
    internal ErlDriverThreadWait420? Thread;
    internal RegisteredWaitHandle? Registration;
    internal bool Completing;
}

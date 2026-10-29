namespace Erlang;

internal sealed record DeferredMailboxAdmission(Term Message, bool Priority = false)
{
    internal RuntimeProcessPayload641? Payload641 { get; init; }
    internal System.Numerics.BigInteger Copies { get; init; } = System.Numerics.BigInteger.One;
}

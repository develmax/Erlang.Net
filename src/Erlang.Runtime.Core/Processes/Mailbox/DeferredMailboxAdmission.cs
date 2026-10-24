namespace Erlang;

internal sealed record DeferredMailboxAdmission(Term Message, bool Priority = false)
{
    internal System.Numerics.BigInteger Copies { get; init; } = System.Numerics.BigInteger.One;
}

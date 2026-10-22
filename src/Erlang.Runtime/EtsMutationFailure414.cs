namespace Erlang;

internal sealed class EtsMutationFailure414(string cause) : Exception
{
    internal string Cause { get; } = cause;
}

namespace Erlang;

internal static class ReceiveTimeoutValue
{
    internal static TimeSpan? Parse(Term timeout) => timeout switch
    {
        Atom { Name: ReceiveTimeoutValueNames.Infinity } => null,
        Integer integer when integer.Value >= 0 && integer.Value <= uint.MaxValue => TimeSpan.FromMilliseconds((double)integer.Value),
        _ => throw new ErlangException(ErlangErrorReasons.TimeoutValue)
    };
}

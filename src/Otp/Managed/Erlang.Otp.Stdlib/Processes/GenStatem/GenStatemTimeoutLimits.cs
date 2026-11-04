namespace Erlang;

internal static class GenStatemTimeoutLimits
{
    // OTP receive-after accepts an unsigned 32-bit millisecond duration.
    internal const uint MaximumHibernateAfterMilliseconds = uint.MaxValue;
}

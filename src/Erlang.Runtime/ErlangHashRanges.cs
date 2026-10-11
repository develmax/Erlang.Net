namespace Erlang;

internal static class ErlangHashRanges
{
    public const ulong Maximum = 1UL << 32;
    public const int DefaultBits = 27;
    public const uint DefaultMask = (1U << DefaultBits) - 1;
}

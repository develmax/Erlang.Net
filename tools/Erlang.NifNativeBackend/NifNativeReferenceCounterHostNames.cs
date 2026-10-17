namespace Erlang.NifNativeBackend;

internal static class NifNativeReferenceCounterHostNames
{
    internal const int LogicalSchedulers = 3;
    internal const int ConcurrentCount = 512;
    internal const int HalfWindow = ConcurrentCount / 2;
    internal const string Counter = "nextRef";
    internal const string Domain = "referenceDomain";
    internal const string Failure = "Reference counter domain boundary contract failed";
    internal static readonly ulong[] Boundaries =
    [
        0, (1UL << 16) - 1, (1UL << 17) - 1, (1UL << 18) - 1,
        uint.MaxValue, long.MaxValue, ulong.MaxValue - 1
    ];
}

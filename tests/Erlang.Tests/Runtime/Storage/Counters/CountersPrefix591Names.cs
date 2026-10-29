using System.Reflection;

namespace Erlang.Tests;

internal static class CountersPrefix591Names
{
    internal const string PrefixCase = "runtime/counters591/actual-binary-prefix-and-source-extents";
    internal const string GeometryCase = "runtime/counters591/cacheline-grouping-and-base-physical-cells";
    internal const string ArrayType = "Erlang.CountersArray421";
    internal const string PrefixType = "Erlang.CountersMagicHeader421";
    internal const string MagicType = "Erlang.NativeResourceMagic527";
    internal const string Magic = "Magic";
    internal const string Memory = "memory";
    internal const string DestructorField = "Destructor";
    internal const string Acquire = "Acquire";
    internal const string Close = "Close";
    internal const string AllocatedBytes = "AllocatedBytes";
    internal const string OriginalSize = "OriginalSize591";
    internal const string Length = "Length";
    internal const string Cell = "Cell";
    internal const string Failure = "Counters591 actual prefix, authoritative reader or physical lane geometry failed.";
    internal const string CleanupFailure = "Counters591 assertions and independent native cleanup failed.";
    internal const int FlagsOffset = 0;
    internal const int ApparentSizeOffset = 8;
    internal const int OriginalSizeOffset = 24;
    internal const int DestructorOffset = 32;
    internal const int ArityOffset = 56;
    internal const int CacheLineBytes = 64;
    internal const int CellsPerLine = CacheLineBytes / sizeof(long);
#if DEBUG
    internal const int CellsOffset = 72;
#else
    internal const int CellsOffset = 64;
#endif
    internal const long Sentinel = 591;
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
}

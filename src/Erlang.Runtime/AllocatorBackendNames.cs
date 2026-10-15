namespace Erlang;

internal static class AllocatorBackendNames
{
    internal const string System = "sys_alloc";
    internal const string Temporary = "temp_alloc";
    internal const string ShortLived = "sl_alloc";
    internal const string Standard = "std_alloc";
    internal const string LongLived = "ll_alloc";
    internal const string ProcessHeap = "eheap_alloc";
    internal const string Ets = "ets_alloc";
    internal const string FixedSize = "fix_alloc";
    internal const string Literal = "literal_alloc";
    internal const string Binary = "binary_alloc";
    internal const string Driver = "driver_alloc";
    internal const string Test = "test_alloc";
    internal const string Mseg = "mseg_alloc";
    internal const string Mapped = "erts_mmap";
    internal const string Utility = "alloc_util";
}

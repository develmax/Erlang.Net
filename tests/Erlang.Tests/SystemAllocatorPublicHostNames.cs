using System.Reflection;

namespace Erlang.Tests;

internal static class SystemAllocatorPublicHostNames
{
    internal const string Test = "runtime/public-allocator-sys-clr-profile-and-complete-validation";
    internal const string Info = "alloc_info";
    internal const string Sizes = "alloc_sizes";
    internal const string System = "sys_alloc";
    internal const string Unsupported = "eheap_alloc";
    internal const string Unknown = "unknown_allocator_public316";
    internal const string Tail = "allocator_public_tail";
    internal const string NotSupported = "notsup";
    internal const string Instance = "instance";
    internal const string Options = "options";
    internal const string Enabled = "e";
    internal const string Mechanism = "m";
    internal const string NativeMemory = "dotnet_native_memory";
    internal const string Memory = "memory";
    internal const string Marker = "public_allocator_retained_marker";
    internal const string ReferenceCounter = "nextRef";
    internal const string Failure = "Public CLR system allocator profile contract failed.";
    internal const int QueryArity = 1;
    internal const int PublicFunctionCount = 2;
    internal const int SystemInstance = 0;
    internal const nuint Initial = 7;
    internal const nuint Grown = 19;
    internal const nuint Shrunk = 3;
    internal const byte PayloadByte = 113;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly string[] RecognizedUnsupported =
    {
        "alloc_util",
        "binary_alloc",
        "driver_alloc",
        "eheap_alloc",
        "erts_mmap",
        "ets_alloc",
        "fix_alloc",
        "literal_alloc",
        "ll_alloc",
        "mseg_alloc",
        "sl_alloc",
        "std_alloc",
        "temp_alloc",
        "test_alloc",
    };
}

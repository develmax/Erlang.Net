namespace Erlang.Tests;

internal static class BeamCollectionFloatControlNames
{
    internal const byte FloatMove = 96;
    internal const byte FloatConvert = 97;
    internal const byte FloatMultiply = 100;
    internal const byte FloatRegister = 0x27;
    internal const byte Call = 4;
    internal const byte ExternalCall = 7;
    internal const byte Allocate = 12;
    internal const byte HeapTest = 16;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte Move = 64;
    internal const string Failure = "BEAM collection floating-register control failed";
    internal const string Prefix = "runtime/library390/float/";
    internal const string LocalStale = Prefix + "local-call-invalidates";
    internal const string LocalFresh = Prefix + "local-call-reinitialize";
    internal const string NativeStale = Prefix + "native-call-invalidates";
    internal const string NativeFresh = Prefix + "native-call-reinitialize";
    internal const string Heap = Prefix + "heap-retains";
    internal const string Uninitialized = Prefix + "uninitialized-refused";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}

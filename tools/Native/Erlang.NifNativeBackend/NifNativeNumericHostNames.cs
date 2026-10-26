namespace Erlang.NifNativeBackend;

internal static class NifNativeNumericHostNames
{
    // Independently fixed expectations from pinned error.h, not production callback constants.
    internal const ulong ExpectedUnsigned64BadArgument = 3152;
    internal const ulong ExpectedUnsigned64SystemLimit = 15440;
    internal const string Module = "nif_numeric_callback_host";
    internal const string Atom = "non_numeric";
    internal const string Failure = "NIF numeric Windows table assertion failed.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    internal static readonly int[] IntegerGetSlots = [11, 68, 69, 98, 99];
    internal static readonly int[] IntegerMakeSlots = [20, 70, 71, 100, 101];
    internal const int UnsignedLongGetSlot = 11;
    internal const int DoubleGetSlot = 12;
    internal const int UnsignedLongMakeSlot = 20;
    internal const int DoubleMakeSlot = 21;
    internal const int UnsignedIntGetSlot = 68;
    internal const int LongGetSlot = 69;
    internal const int UnsignedIntMakeSlot = 70;
    internal const int LongMakeSlot = 71;
    internal const int Int64GetSlot = 98;
    internal const int UInt64GetSlot = 99;
    internal const int Int64MakeSlot = 100;
    internal const int UInt64MakeSlot = 101;
    internal const int ExistingIntGetSlot = 10;
    internal const int ExistingIntMakeSlot = 19;

    internal const int GuardBytes = 8;
    internal const int BufferBytes = 24;
    internal const byte Canary = 165;
}

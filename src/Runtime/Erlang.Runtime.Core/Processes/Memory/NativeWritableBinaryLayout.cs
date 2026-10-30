namespace Erlang;

internal static class NativeWritableBinaryLayout
{
    internal const nuint Writable = 4;
    internal const nuint ActiveWriter = 8;
    internal const nuint WritableActive = Writable | ActiveWriter;
    internal const nuint Mutable = 1;
    internal const nuint Volatile = 2;
    internal const nuint CurrentWriter = Mutable | Volatile;
    internal const int PinSpareBytes = 8;
    internal const int MuchLimit = 1;
    internal const int MuchPercent = 20;
    internal const int LeaveLimit = 10;
    internal const int LeavePercent = 10;
    internal const string Invalid = "The actual writable binary, BinRef, or writer header is invalid, retired, or foreign.";
}

namespace Erlang;

internal static class PersistentTableNames416
{
    internal const ulong Magic = 0x5054424C34313631UL;
    internal const int HeaderBytes = 32;
    internal const int ExtentOffset = 8;
    internal const int CapacityOffset = 16;
    internal const int CountOffset = 20;
    internal const int ActivePairBytesOffset = 24;
    internal const int SlotBytes = 16;
    internal const int InitialCapacity = 8;
    internal const int GrowDivisor = 2;
    internal const int ShrinkDivisor = 4;
    internal const int RetirementUnits = 64;
    internal const string Invalid = "Authoritative persistent table image is invalid";
    internal const string Closed = "Persistent arena table is retired";
}

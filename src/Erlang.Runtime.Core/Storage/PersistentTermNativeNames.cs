namespace Erlang;

internal static class PersistentTermNativeNames
{
    internal const string Module = "persistent_term";
    internal const string Info = "info";
    internal const string Get = "get";
    internal const string Put = "put";
    internal const string PutNew = "put_new";
    internal const string Erase = "erase";
    internal const string ForeignOwner = "Persistent-term primitive belongs to a different runtime";
    internal const string Closed = "The persistent-term storage owner is closed";
    internal const int RetirementUnitsPerTurn = 64;
    internal const int InitialCapacity = 8;
    internal const int GrowDivisor = 2;
    internal const int ShrinkDivisor = 4;
}

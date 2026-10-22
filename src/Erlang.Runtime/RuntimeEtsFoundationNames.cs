namespace Erlang;

internal static class RuntimeEtsFoundationNames
{
    internal const string ForeignActor = "The storage actor belongs to a different runtime";
    internal const string DeadActor = "The storage actor is no longer active";
    internal const string UnsupportedFunction = "Owned function storage requires a reviewed function environment rebinding substrate";
    internal const string UnsupportedTerm = "This host term representation has no owned storage codec";
    internal const string CyclicTerm = "Cyclic host term graphs cannot be stored";
    internal const string InvalidTable = "The internal table identity is absent or retired";
    internal const string AccessDenied = "The internal table protection rejects this operation";
    internal const string NotOwner = "The actor does not own the internal table";
    internal const string NameInUse = "The internal table name is already in use";
    internal const string MissingObject = "The internal stored object slot is absent";
    internal const string ClosedStorage = "The internal storage owner is closed";
}

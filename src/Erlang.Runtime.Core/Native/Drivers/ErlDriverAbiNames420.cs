namespace Erlang;

internal static class ErlDriverAbiNames420
{
    internal const string PointerBytes = "PointerBytes";
    internal const string CallbackBytes = "WinCallbackBytes";
    internal const string CallbackPrefix = "Callback_";
    internal const string EntryPrefix = "Entry_";
    internal const string EntryBytes = "EntryBytes";
    internal const string InvalidLayout = "ErlDrv requires an admitted measured native ABI layout";
    internal const string IncompleteCallbacks = "ErlDrv requires genuine typed callbacks for every active ABI slot";
}

namespace Erlang;

internal static class NifNativeMutex795Names
{
    internal const int CreateSlot = 29;
    internal const int DestroySlot = 30;
    internal const int TryLockSlot = 31;
    internal const int LockSlot = 32;
    internal const int UnlockSlot = 33;
    internal const int NameSlot = 159;
    internal const int Busy = 16;
    internal const int Invalid = 22;
    internal const string Unknown = "unknown";
    internal const string InvalidHandle = "Native mutex handle is absent, foreign or destroyed.";
    internal const string OwnedAllocation = "A native mutex handle must be destroyed through its mutex API.";
    internal const string BusyDestroy = "A native mutex with a holder or waiters cannot be destroyed.";
    internal const string WrongThread = "Only the holding physical thread may unlock this native mutex.";
    internal const string Closed = "Native mutex allocation owner has closed.";
}

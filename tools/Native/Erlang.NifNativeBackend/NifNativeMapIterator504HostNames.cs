namespace Erlang.NifNativeBackend;

internal static class NifNativeMapIterator504HostNames
{
    internal const string Module = "map_iterator504";
    internal const string Failure = "Native map iterator504 state, ownership or borrowed-value contract failed.";
    internal const int CreateSlot = 115;
    internal const int DestroySlot = 116;
    internal const int HeadSlot = 117;
    internal const int TailSlot = 118;
    internal const int NextSlot = 119;
    internal const int PreviousSlot = 120;
    internal const int PairSlot = 121;
    internal const int NewMapSlot = 110;
    internal const int PutSlot = 111;
    internal const int NewBinarySlot = 81;
    internal const int IteratorWords = 7;
    internal const int GuardedWords = IteratorWords + 2;
    internal const nuint Before = 17;
    internal const nuint After = 29;
    internal const nuint Unchanged = 101;
    internal static readonly int[] Sizes = [0, 1, 2, 31, 32, 33, 65];
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}

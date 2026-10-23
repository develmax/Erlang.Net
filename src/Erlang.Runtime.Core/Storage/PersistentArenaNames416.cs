namespace Erlang;

internal static class PersistentArenaNames416
{
    internal const ulong Magic = 0x4152454E41343136UL;
    internal const int HeaderBytes = 40;
    internal const int RootCountOffset = 32;
    internal const int NodeHeaderBytes = 24;
    internal const int OffsetBytes = sizeof(ulong);
    internal const int MissingRoot = -1;
    internal const string Invalid = "Authoritative persistent arena image is invalid";
    internal const string Cycle = "Persistent pair graph is cyclic";
    internal const string Unsupported = "Term has no admitted persistent arena representation";
    internal const string RootKind = "Persistent arena offheap root kind differs";
    internal const string Closed = "Persistent arena owner is retired";
    internal const string Count = "count";
    internal const string Memory = "memory";
}

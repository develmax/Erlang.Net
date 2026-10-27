namespace Erlang.Tests;

internal static class WindowsVariadic526Names
{
    internal const string Test = "runtime/nif526/windows-variadic-slots-never-null";
    internal const string Table = "Erlang.NifNativeWindowsCallbacks";
    internal const string PointerProperty = "Pointer";
    internal const string Failure = "Official Windows enif_make_tuple/list callback slots must be actual C variadic entry points.";
    internal const int TupleSlot = 24;
    internal const int ListSlot = 25;
    internal const int ThreadSlot = 139;
}

namespace Erlang;

internal static class NifNativeWindowsCallbackLayout
{
    internal const int FunctionCount = 180;
    internal const int TailPointers = 2;
    internal const int PrivateData = 0;
    internal const int Allocate = 1;
    internal const int Free = 2;
    internal const int Reallocate = 58;
    internal const int MakeTupleFromArray = 72;
    internal const int MakeListFromArray = 73;
    internal const int GetInt = 10;
    internal const int BadArgument = 18;
    internal const int MakeInt = 19;
    internal const int MakeString = 27;
    internal const int IsException = 102;
    internal const int HasPendingException = 123;
    internal const int RaiseException = 124;
}

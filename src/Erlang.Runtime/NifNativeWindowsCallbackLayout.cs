namespace Erlang;

internal static class NifNativeWindowsCallbackLayout
{
    internal const int FunctionCount = 180;
    internal const int TailPointers = 2;
    internal const int IsAtom = 3;
    internal const int IsBinary = 4;
    internal const int IsReference = 5;
    internal const int IsFunction = 65;
    internal const int IsPid = 66;
    internal const int IsPort = 67;
    internal const int IsEmptyList = 74;
    internal const int IsList = 82;
    internal const int IsTuple = 83;
    internal const int IsNumber = 104;
    internal const int IsMap = 108;
    internal const int GetListCell = 13;
    internal const int GetUnsignedLong = 11;
    internal const int GetDouble = 12;
    internal const int MakeUnsignedLong = 20;
    internal const int MakeDouble = 21;
    internal const int GetUnsignedInt = 68;
    internal const int GetLong = 69;
    internal const int MakeUnsignedInt = 70;
    internal const int MakeLong = 71;
    internal const int GetInt64 = 98;
    internal const int GetUInt64 = 99;
    internal const int MakeInt64 = 100;
    internal const int MakeUInt64 = 101;
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

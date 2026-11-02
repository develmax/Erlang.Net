namespace Erlang;

internal static class HeapFunctionLayout
{
    internal const int HeaderWords = 1;
    internal const int EntryWords = 1;
    internal const int ArityShift = 8;
    internal const int EnvironmentShift = 16;
    internal const int KindShift = 24;
    internal const nuint Subtag = 5u << 2;
    internal const ulong DispatchSignature = 0x455246554e43414cUL;
    internal const string Invalid = "The function heap header or owned dispatch entry is invalid, foreign, or retired.";

    internal static nuint Header(int arity, int free) => Subtag | (nuint)arity << ArityShift | (nuint)free << EnvironmentShift;

    internal static nuint HeaderForFunction(int arity, int free, bool external)
        => Header(arity, free) | (external ? (nuint)1 << KindShift : 0);

}

namespace Erlang.NifNativeBackend;

internal static class NifNativeBinaryHostNames
{
    internal const int SubviewParentBytes = 96;
    internal const string Module = "nif_binary_callback_host";
    internal const string Atom = "binary_callback_atom";
    internal const string Failure = "NIF binary Windows table assertion failed.";
    internal const string ExpectedExit = "binary cleanup control";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    internal const int Inspect = 6;
    internal const int Allocate = 7;
    internal const int Reallocate = 8;
    internal const int Release = 9;
    internal const int Make = 17;
    internal const int InspectIolist = 61;
    internal const int Sub = 62;
    internal const int New = 81;
    internal const int Tuple = 72;
    internal const int List = 73;
    internal const int TupleGet = 14;
    internal const int ListCell = 13;
    internal const int ListLength = 85;
    internal const int IsBinary = 4;
    internal const int IsTuple = 83;
    internal const int IsList = 82;
    internal const int RaiseException = 124;
    internal const int PendingException = 123;
    internal const int IsException = 102;
    internal const int DeepLevels = 20000;
    internal static readonly nint Spare0 = 123;
    internal static readonly nint Spare1 = 456;
}

namespace Erlang.NifNativeBackend;

internal static class NifNativeImmediateHostNames
{
    internal const string Module = "nif_immediate_numeric_host";
    internal const string WrongType = "not_an_integer";
    internal const string Failure = "NIF immediate numeric contract failed";
    internal const int GetInt = 10;
    internal const int MakeInt = 19;
    internal const int MakeUnsignedLong = 20;
    internal const int MakeUnsignedInt = 70;
    internal const int MakeLong = 71;
    internal const int MakeInt64 = 100;
    internal const int MakeUInt64 = 101;
    internal static readonly int[] ImmediateSlots = [MakeInt, MakeUnsignedLong, MakeUnsignedInt, MakeLong];
}

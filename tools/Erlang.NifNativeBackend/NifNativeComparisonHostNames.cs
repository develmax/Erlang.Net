namespace Erlang.NifNativeBackend;

internal static class NifNativeComparisonHostNames
{
    internal const string Module = "nif_compare_reference_host";
    internal const string Atom = "nif_compare_atom";
    internal const string PeerNode = "nif_compare_peer@fixture";
    internal const string Failure = "NIF comparison/reference function-pointer control failed.";
    internal const int IdenticalSlot = 15;
    internal const int CompareSlot = 16;
    internal const int ReferenceSlot = 28;
    internal const int NewBinarySlot = 81;
    internal const int TupleSlot = 72;
    internal const int ListSlot = 73;
    internal const int DeepLevels = 4096;
    internal const int ReferenceCount = 1024;
}

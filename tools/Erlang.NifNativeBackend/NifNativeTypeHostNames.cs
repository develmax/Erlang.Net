namespace Erlang.NifNativeBackend;

internal static class NifNativeTypeHostNames
{
    internal const string Module = "nif_type_callback_host";
    internal const string Atom = "payload";
    internal const string ExternalNode = "peer@elsewhere";
    internal const string Failure = "NIF type/list-cell table assertion failed.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    internal static readonly int[] Slots = [3, 4, 5, 65, 66, 67, 74, 82, 83, 104, 108];
    internal const int CellSlot = 13;
}

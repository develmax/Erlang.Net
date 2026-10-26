namespace Erlang;

internal static class ErlangLoadNifNames
{
    internal const string BadLibrary = "bad_lib";
    internal const string IndirectCallMessage = "load_nif/2 must be explicitly called from the NIF module. It cannot be called through apply/3.";
}

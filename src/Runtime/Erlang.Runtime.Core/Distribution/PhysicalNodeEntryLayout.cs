namespace Erlang;

internal static class PhysicalNodeEntryLayout
{
    // This VM's consumed node ABI, not an asserted ErtsNode struct ABI.
    internal const int NameHandleWord = 0;
    internal const int CreationWord = 1;
    internal const int ReferencesWord = 2;
    internal const int Words = 3;
    internal const string Invalid = "Invalid physical external node entry";
}

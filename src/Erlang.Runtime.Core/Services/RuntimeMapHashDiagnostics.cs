namespace Erlang;

internal static class RuntimeMapHashDiagnostics
{
    internal const string UnplannedKey = "Map hash topology requested a key before its nested layouts were planned.";
    internal const string UnbalancedScope = "Map hash node identity scope was released without admission.";
}

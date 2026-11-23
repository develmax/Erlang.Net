namespace Erlang.Tests;

internal static class BeamCheckedListReturnDiagnostics
{
    internal const string ByteMismatch = "Checked-entry return did not match the pinned BEAM image.";
    internal const string Result = "Checked-entry return changed call order or the constructed value.";
    internal const string EntryMatch = "Checked-entry return ran its body on a rejected argument.";
    internal const string Binding = "Checked-entry return skipped or duplicated an existing variable match.";
}

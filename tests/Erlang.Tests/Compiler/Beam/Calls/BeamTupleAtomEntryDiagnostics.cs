namespace Erlang.Tests;

internal static class BeamTupleAtomEntryDiagnostics
{
    internal const string EntryPartition = "The tuple type must bypass the atom test after a tagged shape failure.";
    internal const string BranchResult = "Disjoint entry routing must retain the selected branch and incoming values.";
    internal const string RejectionLocation = "The unmatched argument must retain the function-clause entry location.";
}

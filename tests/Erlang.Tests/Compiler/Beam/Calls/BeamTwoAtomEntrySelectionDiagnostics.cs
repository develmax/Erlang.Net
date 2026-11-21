namespace Erlang.Tests;

internal static class BeamTwoAtomEntrySelectionDiagnostics
{
    internal const string Table = "The two atom keys must reference their source-derived entry blocks in one selection table.";
    internal const string Layout = "Independent branches must appear in reverse order of their sorted selection keys.";
    internal const string Fallback = "Single-key rejection and the guarded operand must retain their original tests.";
}

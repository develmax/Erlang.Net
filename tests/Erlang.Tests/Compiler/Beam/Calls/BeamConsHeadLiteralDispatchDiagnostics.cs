namespace Erlang.Tests;

internal static class BeamConsHeadLiteralDispatchDiagnostics
{
    internal const string Dispatch = "The atom table must use the extracted list head and share only identical branch bodies.";
    internal const string Fallback = "The cons miss and non-cons edges must retain their individual continuations.";
    internal const string Overlap = "A single key and overlapping guarded clauses must keep their comparisons.";
}

namespace Erlang.Tests;

internal static class BeamPostCallResultAliasDiagnostics
{
    internal const string SoleResult = "Keep the result in X0 and retain the zero-width continuation frame.";
    internal const string TypedRead = "Keep type information on the forwarded result operand and preserve the existing heap roots.";
    internal const string Clobber = "Keep the saved result when an arm overwrites X0 before reading it.";
    internal const string Replacement = "Keep the slot when the old result can be replaced before reading it.";
    internal const string SecondCall = "Keep the saved result across a second ordinary call.";
    internal const string OtherRoot = "Keep a frame that owns another retained value.";
}

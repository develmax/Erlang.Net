namespace Erlang.Tests;

internal static class BeamPostCallResultAliasDiagnostics
{
    internal const string SoleResult = "Keep the result in X0 and retain the zero-width continuation frame.";
    internal const string TypedRead = "Keep type information on the forwarded result operand and preserve the existing heap roots.";
    internal const string Clobber = "Keep the saved result when an arm overwrites X0 before reading it.";
    internal const string Replacement = "Keep the slot when the old result can be replaced before reading it.";
    internal const string SecondCall = "Keep the saved result across a second ordinary call.";
    internal const string ConstructedArguments = "Keep the call result through creation of a terminal list argument.";
    internal const string DifferentReason = "Retain the explicit reason when the comparison does not establish its value.";
    internal const string OwnedBif = "A post-call BIF must consume the original X0 and keep its independent stack operand.";
    internal const string PassThrough = "A standalone saved result reload must retain its existing failure continuation layout.";
    internal const string OtherRoot = "Keep a frame that owns another retained value.";
    internal const string GuardedContinuation = "Only a guard branch reached from the original call may retain its result in X0.";
    internal const string OutgoingConstructor =
        "Keep the outgoing call argument in X0 while constructing its independent sibling.";
    internal const string CheckedRecordField =
        "Keep the original call result rooted until a checked record update consumes its field.";
    internal const string ConstructorSibling = "A sibling constructor did not preserve the immediate call result in X0.";
}

namespace Erlang.Tests;

internal static class BeamClosedBindingDiagnostics
{
    internal const string Compiler = "Erlang.Compiler.BeamModuleCompiler";
    internal const string Emitter = "GeneralEmitter";
    internal const string Fold = "FoldStraightSequenceConstants";
    internal const string MissingFold = "Expected closed tuple and list bindings to fold into their uses.";
    internal const string BindingChanged = "A lexical binding escaped the closed-constant rewrite.";
    internal const string TailArgumentOrder = "Closed tail arguments did not retain the OTP move order.";
    internal const string ChangedResult = "A closed-binding fixture changed its result or pattern matching.";
}

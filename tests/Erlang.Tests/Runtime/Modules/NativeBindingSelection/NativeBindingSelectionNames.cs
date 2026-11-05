namespace Erlang.Tests;

internal static class NativeBindingSelectionNames
{
    internal const string Lists = "lists";
    internal const string Reverse = "reverse";
    internal const string NativeCall = "CallWithNativeCallerScope";
    internal const string BadArgument = "badarg";
    internal const string InvalidList = "invalid_native_reverse_list";
    internal const string Replacement = "replacement_reverse";
    internal const string Resource = "Erlang.Tests.BeamOrdsets381.lists.beam";
    internal const string Failure = "Atomic native selection contract differs";
    internal const string Output = "artifacts/native-binding-selection/results.json";
    internal const string Test = "runtime/lists/native-binding/atomic-selected-caller-scope";
    internal const string RaceTest = "runtime/lists/native-binding/replacement-after-native-marker";
    internal const string Before = "before";
    internal const string After = "after";
    internal const string Control = "control";
    internal const string Erlang = "erlang";
    internal const string Error = "error";
    internal const string Throw = "throw";
    internal const string Yield = "yield";
    internal const string True = "true";
    internal const string Tail = "tail";
    internal const string Union = "union";
    internal const string Ordsets = "ordsets";
    internal const string ResumeModule = "audit_resume";
    internal const string ResumeFunction = "run";
    internal const string ThrowReason = "scope_throw";
    internal const string FactoryCancellation = "scope_factory_cancel";
    internal const string ExecutionFrameField = "executionFrame";
    internal const string ThreadContextChanged = "ThreadContextChanged";
    internal const string CurrentValue = "CurrentValue";
    internal const string ValueProperty = "Value";
    internal const string FunctionProperty = "Function";
    internal const string Helper = "umerge2_2";
    internal const string TraceType = "Erlang.ListUniqueMergeTrace";
    internal const string ContinuationType = "Erlang.ListUniqueMergeContinuation";
    internal const string StateType = "Erlang.ListUniqueMergeState";
    internal const string ReverseMethod = "Reverse";
    internal const string ReverseState = "Umerge2_24";
    internal const int HelperLine = 3471;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal const string OriginalImageSha256 = "8941092DC34F0470CD404979E1890DF514D83F2EEB3B0AA8A276553E76471262";
    internal const int ReverseArity = 2;
}

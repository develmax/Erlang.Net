namespace Erlang.Tests;

internal static class BeamTryHostCaseNames
{
    internal const string NormalBody = "normal_body";
    internal const string ErrorClass = "error_class";
    internal const string ThrowClass = "throw_class";
    internal const string ExitClass = "exit_class";
    internal const string UnmatchedClass = "unmatched_class";
    internal const string NestedLifo = "nested_lifo";
    internal const string HandlerThrows = "handler_throws";
    internal const string TryOfMismatch = "try_of_mismatch";
    internal const string AfterNormal = "after_normal";
    internal const string AfterFailure = "after_failure";
    internal const string AfterReplaces = "after_replaces";
    internal const string ExplicitRaise = "explicit_raise";
    internal const string Reraised = "reraised";
    internal const string CallUnwind = "call_unwind";
    internal const string TailUnwind = "tail_unwind";
    internal const string SavedTrace = "saved_trace";
    internal const string DeepUnwind = "deep_unwind";
    internal static readonly IReadOnlyList<string> Ordered = Array.AsReadOnly(new[]
    {
        NormalBody,
        ErrorClass,
        ThrowClass,
        ExitClass,
        UnmatchedClass,
        NestedLifo,
        HandlerThrows,
        TryOfMismatch,
        AfterNormal,
        AfterFailure,
        AfterReplaces,
        ExplicitRaise,
        Reraised,
        CallUnwind,
        TailUnwind,
        SavedTrace,
        DeepUnwind
    });
}

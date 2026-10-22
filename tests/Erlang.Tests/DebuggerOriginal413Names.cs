namespace Erlang.Tests;

internal static class DebuggerOriginal413Names
{
    internal const string Test = "runtime/library413/original-debugger-default-disabled-thirteen-whole-terms";
    internal const string ResourcePrefix = "Erlang.Tests.Resources.DebuggerOriginal413.";
    internal const string Manifest = "manifest.json";
    internal const string Module = "erl_debugger";
    internal const string Observer = "beam_debugger_observer413";
    internal const string Subject = "beam_debugger_subject413";
    internal const string ArtifactRoot = "artifacts/library413-debugger-permanent-comparisons";
    internal const string GuidFormat = "N";
    internal const string ActualValues = "actual-values.etf";
    internal const string ActualStacks = "actual-stacks.etf";
    internal const string TypedValues = "actual-values.typed.json";
    internal const string TypedStacks = "actual-stacks.typed.json";
    internal const string Receipt = "comparison.json";
    internal const string ValuesScope = "values";
    internal const string StacksScope = "stacks";
    internal const string Failure = "Debugger default-profile complete original comparison failed; actual raw and differences retained";
    internal const int CompleteRows = 78;
    internal const int NativeCount = 13;
    internal static readonly LibraryDependencyOriginalSuite Default = new(
        "debugger413",
        Observer,
        [Observer, Subject],
        [Module],
        13,
        3
    );
}

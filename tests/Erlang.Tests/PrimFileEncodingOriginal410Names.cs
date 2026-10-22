namespace Erlang.Tests;

internal static class PrimFileEncodingOriginal410Names
{
    internal const string ModuleName = "prim_file";
    internal const string ToNative = "internal_name2native";
    internal const int NativeCount = 4;
    internal const string Observer = "beam_prim_file_encoding410";
    internal const string ArtifactRoot = "artifacts/library410-prim-file-encoding-permanent-comparisons";
    internal const string GuidFormat = "N";
    internal const string ActualValues = "actual-values.etf";
    internal const string ActualStacks = "actual-stacks.etf";
    internal const string TypedValues = "actual-values.typed.json";
    internal const string TypedStacks = "actual-stacks.typed.json";
    internal const string ExpectedValues = "expected-values.etf";
    internal const string ExpectedStacks = "expected-stacks.etf";
    internal const string ExpectedTypedValues = "expected-values.typed.json";
    internal const string ExpectedTypedStacks = "expected-stacks.typed.json";
    internal const string Receipt = "comparison.json";
    internal const string AdmissionFailure = "admission-failure.json";
    internal const string PassiveFunction = "An original comparison function description cannot execute guest code";
    internal const string RuntimeModule = "erlang";
    internal const string Encode = "term_to_binary";
    internal const string ValuesScope = "values";
    internal const string StacksScope = "stacks";
    internal const string Failure = "Original prim_file encoding410 complete outcome comparison failed; all differences retained.";
    internal const string Test = "runtime/library410/windows-original-filename-helpers-whole-terms-and-stacks";
    internal const int ProfileCount = 5;
    internal const int CompleteRows = 198;
    internal static readonly LibraryDependencyOriginalSuite Suite = new(
        "prim-file-encoding410",
        Observer,
        [Observer],
        [ModuleName],
        33,
        2
    );
}

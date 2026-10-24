namespace Erlang.Tests;

internal static class RegexThreeOriginal413Names
{
    internal const string ResourcePrefix = "Erlang.Tests.RegexThreeOriginal413.";
    internal const string ResourceManifest = "manifest.json";
    internal const string Module = "re";
    internal const string Erlang = "erlang";
    internal const string Observer = "beam_re_three413";
    internal const string ExportInputs = "export-inputs.etf";
    internal const string ArtifactRoot = "artifacts/library413-re-three-comparisons";
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
    internal const string ValuesScope = "values";
    internal const string Failure = "Original whole re8 missing-three comparison failed; full raw/typed differences retained";
    internal const string Test = "runtime/library413/original-re8-inspect-internal-run-import-full-outcomes";
    internal const int NativeBodies = 8;
    internal const int CompleteRows = 252;
    internal static readonly string[] OriginalModules = ["lists", "binary", "unicode_util", "unicode", Module];
    internal static readonly LibraryDependencyOriginalSuite Suite = new(
        "re-three413",
        Observer,
        [Observer],
        [],
        42,
        2
    );
}

namespace Erlang.Tests;

internal static class InternalScalarOriginal413Names
{
    internal const string ResourcePrefix = "Erlang.Tests.InternalScalarOriginal413.";
    internal const string ResourceManifest = "manifest.json";
    internal const string Erlang = "erlang";
    internal const string NativeModule = "erts_internal";
    internal const string NativeEntryMethod = "IsNativeEntry";
    internal const string Observer = "beam_internal_scalars413";
    internal const string ArtifactRoot = "artifacts/library413-internal-scalars-comparisons";
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
    internal const string Failure = "Original internal scalar413 complete outcome comparison failed; differences retained";
    internal const string Test = "runtime/library413/original-internal-four-whole-values-and-stacks";
    internal const int NativeBodies = 4;
    internal const int CompleteRows = 372;
    internal static readonly string[] OriginalModules = ["lists", "binary"];
    internal static readonly (string Function, int Arity)[] NativeSignatures =
    [
        ("binary_to_integer", 2), ("list_to_integer", 2), ("time_unit", 0), ("perf_counter_unit", 0)
    ];
    internal static readonly LibraryDependencyOriginalSuite Suite = new(
        "internal-scalars413",
        Observer,
        [Observer],
        [],
        62,
        2
    );
}

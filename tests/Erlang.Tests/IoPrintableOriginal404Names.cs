using Erlang;

namespace Erlang.Tests;

internal static class IoPrintableOriginal404Names
{
    internal const string Observer = "beam_printable_observer404";
    internal const string Subject = "beam_printable_subject404";
    internal const string EppModule = "epp";
    internal const string CodeModule = "code";
    internal const string CodeServerBootRoot = "artifacts/library404-code-server-boot";
    internal const string ArtifactRoot = "artifacts/library404-permanent-comparisons";
    internal const string GuidFormat = "N";
    internal const string ActualValues = "actual-values.etf";
    internal const string ActualStacks = "actual-stacks.etf";
    internal const string TypedValues = "actual-values.typed.json";
    internal const string TypedStacks = "actual-stacks.typed.json";
    internal const string Receipt = "comparison.json";
    internal const string ValuesScope = "values";
    internal const string StacksScope = "stacks";
    internal const string FloatBitsFormat = "X16";
    internal const string Failure = "Printable original404 full outcome comparison failed; all differences retained.";
    internal const int ProfileCount = 5;
    internal const int CompleteRows = 180;
    internal const string DefaultTest = "runtime/library404/original-printable-range-default-whole-terms";
    internal const string Latin1Test = "runtime/library404/original-printable-range-latin1-whole-terms";
    internal const string UnicodeTest = "runtime/library404/original-printable-range-unicode-whole-terms";
    internal static readonly LibraryDependencyOriginalSuite Default = new(
        "printable404-default",
        Observer,
        [Observer, Subject],
        [IoPrintableRange404Names.ModuleName, "unicode", EppModule, CodeModule],
        30,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Latin1 = new(
        "printable404-latin1",
        Observer,
        [Observer, Subject],
        [IoPrintableRange404Names.ModuleName, "unicode", EppModule, CodeModule],
        30,
        3,
        PrintableRange: RuntimePrintableRange.Latin1
    );
    internal static readonly LibraryDependencyOriginalSuite Unicode = new(
        "printable404-unicode",
        Observer,
        [Observer, Subject],
        [IoPrintableRange404Names.ModuleName, "unicode", EppModule, CodeModule],
        30,
        3,
        PrintableRange: RuntimePrintableRange.Unicode
    );
}

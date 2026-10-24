namespace Erlang.Tests;

internal static class ProplistFamilyTestNames
{
    internal const string DirectTest = "runtime/proplists-family-ordinary-contracts";
    internal const string CompiledTest = "compiler/proplists-family-compiled-contracts";
    internal const string AstTest = "compiler/source-fun-continuation-binding-safety";
    internal const string SafetyTest = "runtime/source-fun-continuation-safety";
    internal const string ProperTest = "runtime/proplists-family-long-proper-unfold";
    internal const string UnfoldTest = "runtime/proplists-family-improper-unfold-continuations";
    internal const string CompactTest = "runtime/proplists-family-improper-compact-continuations";
    internal const string DeleteTest = "runtime/proplists-family-improper-delete-continuations";
    internal const string CancellationTest = "runtime/proplists-family-traversal-cancellation";
    internal const string CasesResource = "Erlang.Tests.ProplistFamily.cases.json";
    internal const string ProbeResource = "Erlang.Tests.ProplistFamily.proplists_family_probe.erl";
    internal const string SafetyResource = "Erlang.Tests.ProplistFamily.continuation_safety.erl";
    internal const string ProbeFile = "proplists_family_probe.erl";
    internal const string Module = "proplists";
    internal const string GetValue = "get_value";
    internal const string CaseEntry = "run_case";
    internal const string Unordered = "unordered_list";
    internal const string ErrorClass = "error";
    internal const string Undefined = "undef";
    internal const string MissingResource = "Missing proplist family test resource: ";
    internal const string Failure = "Proplist family integration checks failed.";
    internal const string OutcomeFailure = "Observed outcome differs from frozen case expectation: ";
    internal const string MissingExport = "Family MFA export missing: ";
    internal const string WrongArity = "Wrong arity was not rejected: ";
    internal const int ExpectedCases = 148;
    internal const int ExpectedMfas = 22;
    internal const int ExpectedAstChecks = 7;
    internal const int ExpectedRuntimeChecks = 6;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}

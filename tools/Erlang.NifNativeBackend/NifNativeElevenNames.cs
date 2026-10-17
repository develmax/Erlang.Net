namespace Erlang.NifNativeBackend;

internal static class NifNativeElevenNames
{
    internal const string Mode = "--reviewed-entry-case";
    internal const string OrdinaryHostMode = "--ordinary-host-entry-case";
    internal const string Admission = "APPROVED_EXACT_NATIVE_ENTRY_EXECUTION";
    internal const string Ok = "ok";
    internal const string Error = "error";
    internal const string Load = "load";
    internal const string LoadVeto = "load_veto";
    internal const string Identity = "identity";
    internal const string Marker = "marker";
    internal const string Third = "third";
    internal const string Ordinary = "ordinary";
    internal const string Untouched = "ordinary_untouched";
    internal const string NotLoaded = "not_loaded";
    internal const string Payload = "payload";
    internal const string Alpha = "alpha_no_declarations";
    internal const string AbsentLoad = "beta_absent_load";
    internal const string Result = "entry_case";
    internal const string LoadResult = "load-result.etf";
    internal const string Raw = "observed.etf";
    internal const string LinuxBuildStatus = "PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD";
    internal const string WindowsBuildStatus = "PASS_OWNED_WINDOWS_NATIVE_ENTRY_FIXTURE_BUILD";
    internal const string WindowsPlatform = "windows-x64-llp64";
    internal const string CasesSha256 = "AFEEFA7ADA24F537042AEC2DFCE4D4673A6194F829B5511206CC9F9E8BD689C6";
    internal const int CaseCount = 11;
    internal const string Veto = "veto";
    internal const string PayloadKey = "alpha";
    internal const int PayloadInteger = 17;
    internal static readonly byte[] PayloadBytes = [1, 2, 3];
    internal const int LoadInfo = 73;
    internal const int AlphaMarker = 41;
    internal const int NoLoadMarker = -1;
    internal const int BetaThird = 233;
    internal const string AlphaThird = "alpha";
    internal const string StubAfterFailure = "stub_after_failure";
    internal static readonly string[] SpecificationKeys = ["Name", "Module", "NativeLeaf", "SourceExpected"];
    internal const string Completion = "completion.json";
}

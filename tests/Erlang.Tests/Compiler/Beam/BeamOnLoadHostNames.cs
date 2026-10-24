namespace Erlang.Tests;

internal static class BeamOnLoadHostNames
{
    internal const string Test = "host/BEAM-pending-on-load-actual-observers";
    internal const string Observer = "beam_on_load_observer";
    internal const string Notice = "on_load_notice";
    internal const string Continue = "on_load_continue";
    internal const string NewImage = "new_image";
    internal const string OldImage = "old_image";
    internal const string Run = "run";
    internal const string Ok = "ok";
    internal const string NotOk = "not_ok";
    internal const string FreshSuccess = "fresh-success";
    internal const string FreshFailure = "fresh-failure";
    internal const string ReplacementSuccess = "replacement-success";
    internal const string ReplacementFailure = "replacement-failure";
    internal const string FreshSuccessModule = "beam_on_load_fresh_success";
    internal const string FreshFailureModule = "beam_on_load_fresh_failure";
    internal const string ReplacementSuccessModule = "beam_on_load_replace_success";
    internal const string ReplacementFailureModule = "beam_on_load_replace_failure";
    internal const string ResourcePrefix = "BeamOnLoad";
    internal const string NewRole = "new";
    internal const string OldRole = "old";
    internal const string Extension = ".beam";
    internal const string NotInvoked = "not_invoked";
    internal const string Error = "error";
    internal const string Undef = "undef";
    internal const string Value = "value";
    internal const string Observed = "observed";
    internal const string Module = "module";
    internal const string OnLoadFailure = "on_load_failure";
    internal const string ActualArtifacts = "artifacts/on-load-actual-summaries";
    internal const string ObservationExtension = ".json";
    internal const string ActualSummary = "actual.etf";
    internal const string UnknownAttribute = "unknown_on_load_metadata";
    internal const string ProofFailure = "Genuine BEAM pending callback host contract failed";
    internal const int WatchdogSeconds = 10;
}

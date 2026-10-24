namespace Erlang.Tests;

internal static class BeamLoaderMfaNames
{
    internal const string Prepare = "mfa/erlang:prepare_loading/2";
    internal const string Query = "mfa/erlang:has_prepared_code_on_load/1";
    internal const string Finish = "mfa/erlang:finish_loading/1";
    internal const string Callback = "mfa/erlang:call_on_load_function/1";
    internal const string Finalize = "mfa/erlang:finish_after_on_load/2";
    internal const string Load = "mfa/erlang:load_module/2";
    internal const string AdmissionHost = "host/loader-typed-admission-and-error-metadata";
    internal const string RaceHost = "host/loader-consumption-and-closure-gate-ordering";
    internal const string PaddedHost = "host/loader-local-reference-canonicalization-and-live-resources";
    internal const string DirectHost = "host/loader-interpreted-generated-and-genuine-BEAM-direct-callbacks";
    internal const string LifecycleHost = "host/loader-ten-native-projections-and-third-generation-prefix-postretry";
}

internal static class IoCompatibilityNames
{
    internal const string Stdlib = "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp";
    internal const string Kernel = "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin";
    internal const string Original = "src/Otp/Original";
    internal const string BeamPattern = "*.beam";
    internal const string BeamExtension = ".beam";
    internal const string Observer = "io_port_cases";
    internal const string Observe = "observe";
    internal const string ModuleInfo = "module_info";
    internal const string ErlangModule = "erlang";
    internal const string Persistent = "persistent_term";
    internal const string Features = "erl_features";
    internal const string Put = "put";
    internal const string SystemFlag = "system_flag";
    internal const string BacktraceFlag = "backtrace_depth";
    internal const string BacktraceField = "BacktraceDepth";
    internal const int DefaultDepth = 8;
    internal const string Uncaught = "uncaught";
    internal const string Exports = "exports.etf";
    internal const string FeatureState = "features.etf";
    internal const string Cases = "cases.etf";
    internal const string Provenance = "provenance.json";
    internal const string ObserverSource = "tools/Validation/Erlang.IoCompatibility/io_port_cases.erl";
    internal const string Suite = "io_SUITE";
    internal const string SuiteSource = "tools/Validation/Erlang.IoCompatibility/Reference/io_SUITE.erl";
    internal const string JsonExtension = ".json";
    internal const string Usage = "Usage: Erlang.IoCompatibility REPOSITORY CORPUS Managed|Beam [scenarios]";
    internal const string Scenarios = "scenarios";
    internal const string ScenarioSuffix = "-scenarios";
    internal const string Preparation = "Preparing ";
    internal const string Scenario = "Scenario ";
    internal const string PublicationFailure = "I/O fixture publication failed";
    internal const string ExportFailure = "I/O export inventory differs from OTP";
    internal const string OwnerFailure = "I/O backend owner differs from the requested selection";
    internal const string ProvenanceFailure = "I/O corpus/source identity changed";
    internal const string BaselineCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal static readonly string[] SuiteDependencies = ["ct", "test_server", "error_info_lib"];
    internal static readonly string[] Helpers =
    [
        "io_lib", "io_lib_format", "io_lib_pretty", "io_lib_fread", "erl_scan", "erl_parse", "erl_anno",
        Features, "epp", "code", "unicode", "unicode_util", "net_kernel", "lists", "maps", "string", "proplists", Suite
    ];
}

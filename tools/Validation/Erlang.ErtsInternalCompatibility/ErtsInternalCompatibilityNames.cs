internal static class ErtsInternalCompatibilityNames
{
    internal const string Module = "erts_internal";
    internal const string Observer = "erts_internal_port_cases";
    internal const string Capture = "capture";
    internal const string Scenario = "scenario";
    internal const string Worker = "worker";
    internal const string ScenarioWorker = "scenario_worker";
    internal const string Exports = "exports.etf";
    internal const string Corpus = "original.etf";
    internal const string Effects = "effects.etf";
    internal const string Beam = ".beam";
    internal const string BeamPattern = "*.beam";
    internal const string Erts = "src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam/Otp/ebin";
    internal const string Kernel = "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin";
    internal const string Stdlib = "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp";
    internal const string Source = "src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam/Otp/src/erts_internal.erl";
    internal const string Ets = "ets";
    internal const string Lists = "lists";
    internal const string Debug = "erts_debug";
    internal const string ModuleInfo = "module_info";
    internal const string Baseline = "OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string Usage = "Usage: <repository> <oracle-directory> Managed|Beam";
    internal const string Publication = "The real observer or original library publication failed";
    internal const string Ownership = "The selected library export inventory or source ownership differs";
    internal const string Uncaught = "uncaught";
    internal const string ReportSuffix = "-report.json";
    internal const string UnknownBackend = "Unknown backend";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}

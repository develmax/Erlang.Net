internal static class SupervisorValidationPaths
{
    internal const string Stdlib = "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp";
    internal const string Kernel = "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin";
    internal const string Erts = "src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam/Otp/ebin";
    internal const string BeamPattern = "*.beam";
    internal const string SourcePattern = "*.cs";
    internal const string BeamExtension = ".beam";
    internal const string OriginalBeam = "supervisor.beam";
    internal const string Corpus = "original.etf";
    internal const string Effects = "effects.etf";
    internal const string Exports = "exports.etf";
    internal const string FeatureBootstrap = "feature-bootstrap.etf";
    internal const string Provenance = "provenance.json";
    internal const string ReportSuffix = "-report.json";
    internal const string ScenarioSeparator = "-";
    internal const string ManagedSources = "src/Otp/Managed/Erlang.Otp.Stdlib/Processes/Supervision";
    internal const string BootSubject = "tests/Erlang.Tests/Resources/CodeServerOriginal410/beam_code_server_subject410.beam";
    internal const string ObserverSource = "tools/Validation/Erlang.SupervisorCompatibility/supervisor_port_cases.erl";
    internal const string ObserverBeam = "supervisor_port_cases.beam";
    internal const string CompanionSource = "tools/Validation/Erlang.SupervisorCompatibility/supervisor_port_second.erl";
    internal const string CompanionBeam = "supervisor_port_second.beam";
    internal const string OriginalBeamInputSuffix = "/ebin/supervisor.beam";
}

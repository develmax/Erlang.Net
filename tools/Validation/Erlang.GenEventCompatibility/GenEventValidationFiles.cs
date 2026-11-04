internal static class GenEventValidationFiles
{
    internal const string SourceDirectory = "src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenEvent";
    internal const string ObserverDirectory = "tools/Validation/Erlang.GenEventCompatibility";
    internal const string OriginalDirectory = "src/Otp/Original";
    internal const string FoundationDirectory = "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp";
    internal const string BeamPattern = "*.beam";
    internal const string ErlangPattern = "*.erl";
    internal const string CSharpPattern = "*.cs";
    internal const string BeamExtension = ".beam";
    internal const string OriginalImage = "gen_event.beam";
    internal const string Exports = "exports.etf";
    internal const string Corpus = "original.etf";
    internal const string ObserverImage = "gen_event_port_cases.beam";
    internal const string ReportSuffix = "-report.json";
    internal const string ReportOptionSeparator = "-";
    internal const string JsonExtension = ".json";
}

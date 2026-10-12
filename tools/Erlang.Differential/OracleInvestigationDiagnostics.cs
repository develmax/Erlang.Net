namespace Erlang.Differential;

internal static class OracleInvestigationDiagnostics
{
    public const string Usage = "Usage: Erlang.Differential <erl-executable> <report.json> [--investigate]; oracle must be OTP 29.1.1";
    public const string InvalidNativeResult = "Native investigation must return an outcome and UTF-8 run/0 disassembly";
    public const string Scope = "Separate native compiler/process-context diagnosis; no additional SDK or original corpus coverage; published fixtures and expectations unchanged";
}

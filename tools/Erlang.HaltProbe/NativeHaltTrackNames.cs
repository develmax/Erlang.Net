namespace Erlang.HaltProbe;

internal static class NativeHaltTrackNames
{
    public const string Command = "--halt";
    public const string Usage = "Usage: Erlang.HaltProbe --halt <erl-executable> <report.json> [case-artifacts-directory]";
    public const string Baseline = "OTP-29.1.1";
    public const string Version = "29.1.1";
    public const string DeclaredSourceCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    public const string VersionCommand = "{ok,V}=file:read_file(filename:join([code:root_dir(),\"releases\",erlang:system_info(otp_release),\"OTP_VERSION\"])),io:format(\"~s\",[V]),erlang:halt(0).";
    public const string CasesDirectory = "halt-native-cases";
    public const string LedgerSuffix = ".predeclared.json";
    public const string DumpEnvironment = "ERL_CRASH_DUMP";
    public const string DumpFile = "erl_crash.dump";
    public const string DumpSecondsEnvironment = "ERL_CRASH_DUMP_SECONDS";
    public const string DumpBytesEnvironment = "ERL_CRASH_DUMP_BYTES";
    public const string DumpPublicKeyEnvironment = "ERL_CRASH_DUMP_PUBLIC_KEY";
    public const string DumpSignature = "=erl_crash_dump:0.5\n";
    public const int DumpPrefixLength = 64;
    public const int InfrastructureExit = 2;
    public const int ComparisonFailureExit = 1;
    public const int Planned = 28;
    public const int ComparablePlanned = 26;
    public const int PhysicalPlanned = 2;
}

namespace DistributionPacketContrasts;

internal static class PacketNames
{
    internal const string Pin = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string Tag = "OTP-29.1.1";
    internal const string Version = "29.1.1";
    internal const string Release = "29";
    internal const string Module = "distribution_packet_probe";
    internal const string Source = "distribution_packet_probe.erl";
    internal const string SourceSha256 = "3D260848B3CDE7AED1F92048B9B72CE5BB7900070183B0928987CF164482DD01";
    internal const string Scenario = "pass-through-send-registered";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-packet.json";
    internal const string Stage = "stage";
    internal const string CompleteStage = "complete";
    internal const string Exception = "exception.etf";
    internal const string Runtime = "runtime.etf";
    internal const string BuildLog = "source-build.log";
    internal const string BuildEvidence = "source-build-evidence.json";
    internal const string CompilePrefix = "compile";
    internal const string RuntimePrefix = "runtime";
    internal const string NativePrefix = "process";
    internal const string Evaluation = "evaluation.erl";
    internal const string BinarySuffix = ".beam";
    internal const string Disassembly = "disassembly.txt";
    internal const string DisassemblyPrefix = "disassemble";
    internal const string CliErl = "erl";
    internal const string CliErlc = "erlc";
    internal const string CliNoShell = "-noshell";
    internal const string CliEval = "-eval";
    internal const string CliCodePath = "-pa";
    internal const string CliOutput = "-o";
    internal const string CliWarnings = "-Werror";
    internal const string CliDebug = "+debug_info";
    internal const string ExecutableSuffix = ".exe";
    internal const string Bin = "bin";
    internal const string Releases = "releases";
    internal const string VersionFile = "OTP_VERSION";
    internal const string AllFiles = "*";
    internal const int Arguments = 3;
    internal const int NativeExecutions = 1;
    internal const int TimeoutSeconds = 45;
    internal const int StreamDrainSeconds = 5;
    internal const int CaptureCancellationJoinSeconds = 5;
    internal const int KilledProcessWaitSeconds = 5;
    internal static readonly string[] RequiredArtifacts =
    [
        "inputs.etf",
        "incoming-send.packet",
        "incoming-send-result.etf",
        "incoming-send-delivered.etf",
        "incoming-reg-send.packet",
        "incoming-reg-send-result.etf",
        "incoming-reg-send-delivered.etf",
        "outgoing-reg-send.packet",
        "outgoing-reg-send-control.etf",
        "stats.etf",
        Stage,
        "payload.etf",
        "identities.etf",
        "incoming-send-control.etf",
        "incoming-reg-send-control.etf",
        "cleanup-controller.etf",
        "kernel-notice.etf"
    ];
}

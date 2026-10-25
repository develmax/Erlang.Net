namespace Erlang.HaltProbe;

internal sealed record NativeHaltTrackRow(
    HaltSubprocessContract Contract,
    HaltSubprocessResult Actual,
    IReadOnlyList<string>? Errors,
    NativeHaltDumpEvidence? Dump = null
);

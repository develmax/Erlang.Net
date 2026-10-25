namespace NodeIdentityContrasts;

internal static class NodeDiagnostics
{
    internal const string Usage = "Usage: Erlang.NodeIdentityContrasts otpRoot freshOutput sourceBuildEvidence.json";
    internal const string Evidence = "Pinned source-built runtime evidence does not match";
    internal const string Source = "Probe source or declared scenario binding does not match";
    internal const string FreshOutput = "Observation output directory must be new";
    internal const string ProcessStart = "Cannot start native process";
    internal const string Runtime = "Pinned optimized JIT runtime attestation was not captured";
    internal const string Compile = "Original probe compilation or disassembly did not complete";
    internal const string Capture = "One or more node identity observations were not completely captured";
}

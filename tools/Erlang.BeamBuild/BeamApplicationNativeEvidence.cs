namespace Erlang.BeamBuild;

internal sealed record BeamApplicationNativeEvidence(
    string AdmissionReceiptSha256,
    string CompilerSha256,
    string RuntimeSha256,
    string DriverSourceSha256,
    string DriverBeamSha256,
    string ParsedEtfSha256,
    string ParsedEtfPath,
    string ToolAssemblySha256
);

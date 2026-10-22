namespace Erlang.BeamBuild;

internal sealed record BeamCompilerProvider(
    string InstalledRoot,
    string ProvenanceKind,
    string AdmissionReceipt,
    string AdmissionReceiptSha256,
    string CompilerSha256
);

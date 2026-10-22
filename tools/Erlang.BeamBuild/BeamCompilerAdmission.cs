using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class BeamCompilerAdmission
{
    internal static AdmittedCompiler Admit(BeamBuildRequest request)
        => Admit(new BeamCompilerProvider(
            request.InstalledRoot,
            request.ProvenanceKind,
            request.AdmissionReceipt,
            request.AdmissionReceiptSha256,
            request.CompilerSha256
        ));

    internal static AdmittedCompiler Admit(BeamCompilerProvider provider)
    {
        byte[] evidenceBytes = File.ReadAllBytes(provider.AdmissionReceipt);
        using var evidence = JsonDocument.Parse(evidenceBytes);
        JsonElement proof = evidence.RootElement;
        if (!BeamBuildStorage.Hash(evidenceBytes).Equals(provider.AdmissionReceiptSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);
        string logSha = provider.ProvenanceKind == BeamBuildProvenanceNames.SourceBuild
            ? SourceBuildReceipt.Validate(proof, provider.InstalledRoot)
            : OfficialReleaseReceipt.Validate(proof, provider.InstalledRoot);
        string executionManifest = proof.GetProperty(BeamCompilerReceiptFields.ExecutionManifestPath).GetString()!;
        string executionSha = proof.GetProperty(BeamCompilerReceiptFields.ExecutionManifestSha256).GetString()!;
        CompilerExecutionClosure.Validate(provider.InstalledRoot, executionManifest, executionSha);
        string version = Path.Combine(
            provider.InstalledRoot,
            BeamCompilerLayoutNames.ReleasesDirectory,
            BeamBuildNames.Release,
            BeamCompilerLayoutNames.VersionFile
        );
        if (File.ReadAllText(version).Trim() != BeamBuildNames.Version)
            throw new InvalidDataException(BeamBuildNames.Admission);
        string executable = Path.Combine(
            provider.InstalledRoot,
            BeamCompilerLayoutNames.BinaryDirectory,
            OperatingSystem.IsWindows() ? BeamCompilerLayoutNames.WindowsCompiler : BeamCompilerLayoutNames.UnixCompiler
        );
        string actual = BeamBuildStorage.HashFile(executable);
        if (!actual.Equals(provider.CompilerSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);

        return new(
            executable,
            actual,
            BeamBuildStorage.Hash(evidenceBytes),
            logSha,
            provider.InstalledRoot,
            executionManifest,
            executionSha,
            provider.ProvenanceKind
        );
    }
}

namespace Erlang.BeamBuild;

internal static class BeamEppCachedEvidence
{
    internal static bool Validate(
        BeamEppNativeEvidence? evidence,
        BeamBuildRequest request,
        BeamDeclaredInputSnapshot snapshot,
        BeamBuildSource source,
        string actualBeam
    )
    {
        if (evidence is null)
            return false;
        if (string.IsNullOrWhiteSpace(evidence.OwnedInputRoot) || !Path.IsPathFullyQualified(evidence.OwnedInputRoot)
            || evidence.Artifacts is not { Length: 3 })
            throw new InvalidDataException(BeamBuildNames.Invalid);
        string stage = Path.GetDirectoryName(Path.GetFullPath(evidence.OwnedInputRoot))!;
        string stageName = Path.GetFileName(stage);
        if (!stageName.StartsWith(BeamBuildArtifactNames.StagePrefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(stageName[BeamBuildArtifactNames.StagePrefix.Length..], BeamBuildArtifactNames.GuidCompactFormat, out _))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        Same(Path.GetDirectoryName(stage)!, request.OutputDirectory);
        Same(evidence.OwnedInputRoot, Path.Combine(stage, BeamEppDriverNames.InputsDirectory));
        string moduleStage = Path.Combine(stage, BeamEppDriverNames.ModulesDirectory, source.Module);
        Same(evidence.RequestPath, Path.Combine(moduleStage, BeamEppDriverNames.RequestFile));
        Same(evidence.ReportPath, Path.Combine(moduleStage, BeamEppDriverNames.OutputDirectory, BeamEppDriverNames.ReportFile));
        string[] artifactNames =
        [
            BeamEppEvidenceNames.EppResultFile,
            BeamEppEvidenceNames.ValidatedFormsFile,
            BeamEppEvidenceNames.CompileResultFile
        ];
        for (int index = 0; index < artifactNames.Length; index++)
        {
            BeamEppArtifactIdentity artifact = evidence.Artifacts[index];
            if (artifact is null || artifact.Bytes < 0 || artifact.Sha256 is not { Length: 64 }
                || artifact.Sha256.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException(BeamBuildNames.Invalid);
            Same(artifact.Path, Path.Combine(moduleStage, BeamEppDriverNames.OutputDirectory, artifactNames[index]));
        }
        BeamDeclaredPaths.RequireNoReparse(stage);
        if (!Directory.Exists(evidence.OwnedInputRoot) || !File.Exists(evidence.RequestPath)
            || !File.Exists(evidence.ReportPath) || evidence.Artifacts.Any(row => !File.Exists(row.Path)))
            return false;
        snapshot.ValidateOwnedTree(evidence.OwnedInputRoot);
        BeamEppNativeEvidence actual = BeamEppEvidenceValidator.Validate(
            evidence.RequestPath,
            evidence.RequestSha256,
            evidence.ReportPath,
            evidence.OwnedInputRoot,
            BeamDeclaredPaths.Relative(request.SourceRoot!, source.Path),
            source.Module,
            request.IncludeDirectories!,
            request.Defines!,
            snapshot.Inventory,
            actualBeam,
            evidence.ReportSha256
        );
        if (!actual.Artifacts.SequenceEqual(evidence.Artifacts))
            throw new InvalidDataException(BeamBuildNames.Invalid);

        return true;
    }

    private static void Same(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) || !Path.IsPathFullyQualified(actual)
            || !Path.TrimEndingDirectorySeparator(Path.GetFullPath(actual)).Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(expected)),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            ))
            throw new InvalidDataException(BeamBuildNames.Invalid);
    }
}

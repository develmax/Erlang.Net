using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class BeamEppEvidenceValidator
{
    internal static BeamEppNativeEvidence Validate(
        string requestPath,
        string knownRequestSha256,
        string reportPath,
        string ownedInputRoot,
        string logicalSource,
        string module,
        IReadOnlyList<string> includes,
        IReadOnlyList<BeamBuildDefine> defines,
        IReadOnlyList<BeamDeclaredFileIdentity> inventory,
        string actualBeamPath,
        string? knownReportSha256 = null
    )
    {
        requestPath = RequireAbsoluteFile(requestPath);
        reportPath = RequireAbsoluteFile(reportPath);
        actualBeamPath = RequireAbsoluteFile(actualBeamPath);
        if (!Path.IsPathFullyQualified(ownedInputRoot))
            throw Invalid();
        ownedInputRoot = Path.GetFullPath(ownedInputRoot);
        BeamDeclaredPaths.RequireNoReparse(ownedInputRoot);
        if (!Directory.Exists(ownedInputRoot) || IsUnder(requestPath, ownedInputRoot)
            || IsUnder(reportPath, ownedInputRoot) || IsUnder(actualBeamPath, ownedInputRoot))
            throw Invalid();
        string requestSha = BeamBuildStorage.HashFile(requestPath);
        string reportSha = BeamBuildStorage.HashFile(reportPath);
        if (requestSha != knownRequestSha256 || knownReportSha256 is not null && reportSha != knownReportSha256)
            throw Invalid();
        using var requestDocument = JsonDocument.Parse(File.ReadAllBytes(requestPath));
        using var reportDocument = JsonDocument.Parse(File.ReadAllBytes(reportPath));
        JsonElement request = requestDocument.RootElement;
        JsonElement report = reportDocument.RootElement;
        RequireUniqueProperties(request);
        RequireUniqueProperties(report);
        RequireKeys(
            request,
            BeamEppEvidenceNames.Source,
            BeamEppEvidenceNames.Module,
            BeamEppEvidenceNames.Includes,
            BeamEppEvidenceNames.Defines,
            BeamEppEvidenceNames.DeclaredFiles,
            BeamEppEvidenceNames.Output
        );
        string reportDirectory = Path.GetDirectoryName(reportPath)!;
        if (!SamePath(Text(request, BeamEppEvidenceNames.Output), reportDirectory)
            || Text(request, BeamEppEvidenceNames.Source) != logicalSource
            || Text(request, BeamEppEvidenceNames.Module) != module
            || Text(report, BeamEppEvidenceNames.RequestSha256) != knownRequestSha256
            || Text(report, BeamEppEvidenceNames.Source) != logicalSource
            || Text(report, BeamEppEvidenceNames.Module) != module
            || !SamePath(Text(report, BeamEppEvidenceNames.InputRoot), ownedInputRoot))
            throw Invalid();
        RequireStrings(request.GetProperty(BeamEppEvidenceNames.Includes), includes);
        RequireStrings(report.GetProperty(BeamEppEvidenceNames.Includes), includes);
        BeamBuildDefine[]? requestDefines = request.GetProperty(BeamEppEvidenceNames.Defines).Deserialize<BeamBuildDefine[]>(
            new JsonSerializerOptions { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow });
        if (requestDefines is null || !requestDefines.SequenceEqual(defines))
            throw Invalid();
        RequireInventory(request.GetProperty(BeamEppEvidenceNames.DeclaredFiles), inventory, withBytes: false);
        RequireInventory(report.GetProperty(BeamEppEvidenceNames.InputFiles), inventory, withBytes: true);
        if (Text(report, BeamEppDriverNames.StatusField) != BeamEppDriverNames.Passed
            || !report.GetProperty(BeamEppDriverNames.CompileInvokedField).GetBoolean()
            || !report.GetProperty(BeamEppDriverNames.FormsUnchangedField).GetBoolean()
            || !report.GetProperty(BeamEppDriverNames.InputsUnchangedField).GetBoolean()
            || !report.GetProperty(BeamEppDriverNames.AnnotationUnchangedField).GetBoolean()
            || Text(report, BeamEppDriverNames.CompilerEntryField) != BeamEppDriverNames.CompilerEntry)
            throw Invalid();
        RequireStrings(
            report.GetProperty(BeamEppEvidenceNames.FixedCompileOptions),
            [BeamEppEvidenceNames.BinaryOption, BeamEppEvidenceNames.ReturnErrorsOption, BeamEppEvidenceNames.ReturnWarningsOption]
        );
        string beamName = module + BeamBuildNames.BeamExtension;
        if (Path.GetFileName(actualBeamPath) != beamName
            || Text(report, BeamEppDriverNames.BeamFileField) != beamName
            || Text(report, BeamEppDriverNames.BeamShaField) != BeamBuildStorage.HashFile(actualBeamPath)
            || report.GetProperty(BeamEppEvidenceNames.BeamBytes).GetInt64() != new FileInfo(actualBeamPath).Length)
            throw Invalid();
        BeamEppArtifactIdentity[] artifacts =
        [
            Artifact(
                report,
                BeamEppEvidenceNames.EppResult,
                BeamEppEvidenceNames.EppResultFile,
                reportDirectory
            ),
            Artifact(
                report,
                BeamEppEvidenceNames.ValidatedForms,
                BeamEppEvidenceNames.ValidatedFormsFile,
                reportDirectory
            ),
            Artifact(
                report,
                BeamEppEvidenceNames.CompileResult,
                BeamEppEvidenceNames.CompileResultFile,
                reportDirectory
            )
        ];
        if (BeamBuildStorage.HashFile(requestPath) != requestSha || BeamBuildStorage.HashFile(reportPath) != reportSha)
            throw Invalid();

        return new(
            requestPath,
            requestSha,
            reportPath,
            reportSha,
            ownedInputRoot,
            artifacts
        );
    }

    private static BeamEppArtifactIdentity Artifact(
        JsonElement report,
        string field,
        string expectedFile,
        string output
    )
    {
        JsonElement row = report.GetProperty(field);
        RequireKeys(
            row,
            BeamEppEvidenceNames.ArtifactPath,
            BeamEppEvidenceNames.ArtifactBytes,
            BeamEppEvidenceNames.ArtifactSha256
        );
        if (Text(row, BeamEppEvidenceNames.ArtifactPath) != expectedFile)
            throw Invalid();
        string path = RequireAbsoluteFile(Path.Combine(output, expectedFile));
        long bytes = new FileInfo(path).Length;
        string sha = BeamBuildStorage.HashFile(path);
        if (row.GetProperty(BeamEppEvidenceNames.ArtifactBytes).GetInt64() != bytes
            || Text(row, BeamEppEvidenceNames.ArtifactSha256) != sha)
            throw Invalid();

        return new(path, bytes, sha);
    }

    private static void RequireInventory(JsonElement rows, IReadOnlyList<BeamDeclaredFileIdentity> expected, bool withBytes)
    {
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != expected.Count)
            throw Invalid();
        var pending = expected.ToDictionary(file => file.LogicalPath, StringComparer.Ordinal);
        foreach (JsonElement row in rows.EnumerateArray())
        {
            if (withBytes)
                RequireKeys(
                    row,
                    BeamEppEvidenceNames.InputPath,
                    BeamEppEvidenceNames.InputBytes,
                    BeamEppEvidenceNames.InputSha256
                );
            else
                RequireKeys(row, BeamEppEvidenceNames.InputPath, BeamEppEvidenceNames.InputSha256);
            if (!pending.Remove(Text(row, BeamEppEvidenceNames.InputPath), out BeamDeclaredFileIdentity? file)
                || Text(row, BeamEppEvidenceNames.InputSha256) != file.Sha256
                || withBytes && row.GetProperty(BeamEppEvidenceNames.InputBytes).GetInt64() != file.Bytes)
                throw Invalid();
        }
        if (pending.Count != 0)
            throw Invalid();
    }

    private static void RequireStrings(JsonElement rows, IReadOnlyList<string> expected)
    {
        if (rows.ValueKind != JsonValueKind.Array
            || !rows.EnumerateArray().Select(row => row.GetString() ?? throw Invalid()).SequenceEqual(expected, StringComparer.Ordinal))
            throw Invalid();
    }

    private static void RequireKeys(JsonElement value, params string[] keys)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .SequenceEqual(keys.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw Invalid();
    }

    private static void RequireUniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw Invalid();
                RequireUniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in value.EnumerateArray())
                RequireUniqueProperties(child);
        }
    }

    private static string Text(JsonElement value, string name)
        => value.GetProperty(name).GetString() ?? throw Invalid();

    private static string RequireAbsoluteFile(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            throw Invalid();
        string full = Path.GetFullPath(path);
        BeamDeclaredPaths.RequireNoReparse(full);
        if (!File.Exists(full))
            throw Invalid();

        return full;
    }

    private static bool SamePath(string first, string second)
        => Path.IsPathFullyQualified(first) && Path.IsPathFullyQualified(second)
            && Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)).Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            );

    private static bool IsUnder(string path, string root)
    {
        string parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string boundary = Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return Path.GetFullPath(path).Equals(parent, comparison) || Path.GetFullPath(path).StartsWith(boundary, comparison);
    }

    private static InvalidDataException Invalid() => new(BeamBuildNames.Invalid);
}

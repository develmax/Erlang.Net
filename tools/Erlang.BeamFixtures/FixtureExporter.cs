using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeamFixtureExport;

internal static class FixtureExporter
{
    public static async Task Run(string[] arguments)
    {
        if (arguments.Length is not (FixtureArgumentLayout.RequiredArguments or FixtureArgumentLayout.ArgumentsWithExecution) || arguments.Length == FixtureArgumentLayout.ArgumentsWithExecution && arguments[FixtureArgumentLayout.ExecuteOption] != FixtureExecutionNames.Option)
        {
            throw new ArgumentException(FixtureExecutionNames.Usage);
        }
        bool execute = arguments.Length == FixtureArgumentLayout.ArgumentsWithExecution;
        string root = Path.GetFullPath(arguments[FixtureArgumentLayout.OracleRoot]);
        string output = Path.GetFullPath(arguments[FixtureArgumentLayout.NewOutput]);
        string evidencePath = Path.GetFullPath(arguments[FixtureArgumentLayout.BuildEvidence]);
        string extension = OperatingSystem.IsWindows() ? FixtureExportNames.ExecutableExtension : "";
        string erl = Path.Combine(root, FixtureExportNames.BinaryDirectory, FixtureExportNames.OracleExecutable + extension);
        string erlc = Path.Combine(root, FixtureExportNames.BinaryDirectory, FixtureExportNames.CompilerExecutable + extension);
        if (!File.Exists(erl) || !File.Exists(erlc))
        {
            throw new FileNotFoundException(FixtureExportDiagnostics.MissingOracle);
        }
        string versionPath = Path.Combine(
            root,
            FixtureExportNames.ReleasesDirectory,
            OraclePin.Release,
            FixtureExportNames.VersionFilename
        );
        if (!File.Exists(versionPath) || (await File.ReadAllTextAsync(versionPath)).Trim() != OraclePin.Version)
        {
            throw new InvalidDataException(FixtureExportDiagnostics.InvalidVersion(OraclePin.Version));
        }
        byte[] evidenceBytes = await File.ReadAllBytesAsync(evidencePath);
        using var evidence = JsonDocument.Parse(evidenceBytes);
        JsonElement proof = evidence.RootElement;
        if (proof.GetProperty(SourceBuildEvidenceNames.SourceCommit).GetString() != OraclePin.Commit
            || proof.GetProperty(SourceBuildEvidenceNames.SourceTag).GetString() != OraclePin.Tag
            || !proof.GetProperty(SourceBuildEvidenceNames.Completed).GetBoolean()
            || !Path.GetFullPath(proof.GetProperty(SourceBuildEvidenceNames.InstalledRoot).GetString()!).Equals(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidDataException(FixtureExportDiagnostics.InvalidEvidence);
        }
        string buildLog = Path.GetFullPath(proof.GetProperty(SourceBuildEvidenceNames.BuildLog).GetString()!);
        if (Hash(await File.ReadAllBytesAsync(buildLog)) != proof.GetProperty(SourceBuildEvidenceNames.BuildLogHash).GetString()?.ToUpperInvariant())
        {
            throw new InvalidDataException(FixtureExportDiagnostics.BuildLogMismatch);
        }
        if (Directory.Exists(output))
        {
            throw new IOException(FixtureExportDiagnostics.ExistingOutput);
        }
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, FixtureExportNames.BuildEvidenceFilename), evidenceBytes);
        await File.WriteAllBytesAsync(Path.Combine(output, FixtureExportNames.BuildLogFilename), await File.ReadAllBytesAsync(buildLog));
        string version = await OracleCommand.Run(
            erl,
            [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, FixtureExportNames.VersionExpression],
            Path.Combine(output, FixtureExportNames.OracleVersionLogPrefix)
        );
        string[] flags = [FixtureExportNames.WarningsAsErrors, FixtureExportNames.DebugInformation];
        var fixtures = new List<object>();
        foreach (FixtureDefinition fixture in FixtureCases.All.Concat(FixtureClosureCases.All))
        {
            string sourcePath = Path.Combine(output, fixture.Module + FixtureExportNames.SourceExtension);
            await File.WriteAllTextAsync(sourcePath, fixture.Source.Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
            await OracleCommand.Run(
                erlc,
                [.. flags, FixtureExportNames.OutputDirectoryOption, output, sourcePath],
                Path.Combine(output, fixture.Module + FixtureExportNames.CompilationLogSuffix)
            );
            string beamPath = Path.Combine(output, fixture.Module + FixtureExportNames.BeamExtension);
            byte[] beam = await File.ReadAllBytesAsync(beamPath);
            string disassemblyPath = Path.Combine(output, fixture.Module + FixtureExportNames.DisassemblyExtension);
            string expression = $"D=beam_disasm:file({ErlangString(beamPath)}),case D of {{beam_file,_,_,_,_,_}} -> ok=file:write_file({ErlangString(disassemblyPath)},unicode:characters_to_binary(io_lib:format(\"~tp.~n\",[D]))),halt(0); _ -> io:format(\"~tp~n\",[D]),halt(1) end.";
            await OracleCommand.Run(
                erl,
                [OracleCommandNames.NoShell, OracleCommandNames.Evaluate, expression],
                Path.Combine(output, fixture.Module + FixtureExportNames.DisassemblyLogSuffix)
            );
            FixtureMetadataEvidence metadata = await FixtureMetadata.Run(
                erl,
                output,
                fixture,
                beamPath
            );
            FixtureExecutionEvidence execution = await FixtureExecution.Run(
                erl,
                output,
                fixture,
                execute
            );
            fixtures.Add(new
            {
                fixture.Module,
                fixture.Purpose,
                SourceFile = Path.GetFileName(sourcePath),
                SourceSha256 = Hash(await File.ReadAllBytesAsync(sourcePath)),
                BeamFile = Path.GetFileName(beamPath),
                BeamBytes = beam.Length,
                BeamSha256 = Hash(beam),
                Chunks = BeamChunkReader.Read(beam),
                DisassemblyFile = Path.GetFileName(disassemblyPath),
                DisassemblySha256 = Hash(await File.ReadAllBytesAsync(disassemblyPath)),
                Metadata = metadata,
                Execution = execution
            });
        }
        var manifest = new
        {
            SourcePin = OraclePin.Commit,
            SourceTag = OraclePin.Tag,
            InstalledVersion = OraclePin.Version,
            InstalledOtpRoot = root,
            OracleVersionRaw = version,
            CompilerFlags = flags,
            CompilerExecutableSha256 = Hash(await File.ReadAllBytesAsync(erlc)),
            OracleExecutableSha256 = Hash(await File.ReadAllBytesAsync(erl)),
            SourceBuildEvidenceSha256 = Hash(evidenceBytes),
            SourceBuildLogSha256 = Hash(await File.ReadAllBytesAsync(buildLog)),
            ProvenanceScope = FixtureExportNames.ProvenanceScope,
            LoaderExecutionPerformed = false,
            OriginalFixtureExecutionRequested = execute,
            OriginalFixturesExecuted = execute ? FixtureCases.All.Concat(FixtureClosureCases.All).Count(fixture => fixture.IndependentExpectedExpression is not null) : 0,
            Fixtures = fixtures
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, FixtureExportNames.ManifestFilename),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n"
        );
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string ErlangString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}

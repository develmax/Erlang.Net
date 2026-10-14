using System.Security.Cryptography;
using System.Text.Json;
namespace ApplyOptimizerContrasts;

internal static class ApplyExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != ApplyNames.ArgumentCount)
            throw new ArgumentException(ApplyDiagnostics.Usage);
        string root = Path.GetFullPath(arguments[0]);
        string repository = Path.GetFullPath(arguments[1]);
        string output = Path.GetFullPath(arguments[2]);
        using var evidence = JsonDocument.Parse(await File.ReadAllBytesAsync(arguments[3]));
        JsonElement proof = evidence.RootElement;
        if (proof.GetProperty(ApplyEvidenceFields.SourceCommit).GetString() != ApplyNames.Commit || proof.GetProperty(ApplyEvidenceFields.SourceTag).GetString() != ApplyNames.Tag || !proof.GetProperty(ApplyEvidenceFields.SourceBuildCompleted).GetBoolean() || Path.GetFullPath(proof.GetProperty(ApplyEvidenceFields.InstalledOtpRoot).GetString()!) != root)
            throw new InvalidDataException(ApplyDiagnostics.Evidence);
        string log = proof.GetProperty(ApplyEvidenceFields.SourceBuildLog).GetString()!;
        byte[] buildLog = await File.ReadAllBytesAsync(log);
        if (Hash(buildLog) != proof.GetProperty(ApplyEvidenceFields.SourceBuildLogSha256).GetString() || (await File.ReadAllTextAsync(Path.Combine(
            root,
            ApplyArtifactNames.Releases,
            ApplyNames.Release,
            ApplyArtifactNames.VersionFile
        ))).Trim() != ApplyNames.Version)
            throw new InvalidDataException(ApplyDiagnostics.Evidence);
        byte[] input = await File.ReadAllBytesAsync(Path.Combine(repository, ApplyNames.Input));
        if (Hash(input) != ApplyNames.InputSha256)
            throw new InvalidDataException(ApplyDiagnostics.Input);
        byte[] declarations = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, ApplyNames.Declarations));
        using var declared = JsonDocument.Parse(declarations);
        string[] scenarios = await ApplySourceVerifier.Verify(declared.RootElement);
        if (Directory.Exists(output))
            throw new IOException(ApplyDiagnostics.FreshOutput);
        Directory.CreateDirectory(output);
        string capturedInput = Path.Combine(output, ApplyArtifactNames.InputBinary);
        await File.WriteAllBytesAsync(capturedInput, input);
        await File.WriteAllBytesAsync(Path.Combine(output, ApplyArtifactNames.BuildLog), buildLog);
        await File.WriteAllBytesAsync(Path.Combine(output, ApplyArtifactNames.BuildEvidence), await File.ReadAllBytesAsync(arguments[3]));
        await File.WriteAllBytesAsync(Path.Combine(output, ApplyNames.Declarations), declarations);
        string suffix = OperatingSystem.IsWindows() ? ApplyCliNames.ExecutableSuffix : "";
        string erl = Path.Combine(root, ApplyArtifactNames.BinDirectory, ApplyCliNames.Erl + suffix);
        string erlc = Path.Combine(root, ApplyArtifactNames.BinDirectory, ApplyCliNames.Erlc + suffix);
        string version = ApplyModuleSource.Runtime(output, root);
        ApplyProcessResult runtime = await Execute(erl, [ApplyCliNames.NoShell, ApplyCliNames.Evaluate, version], Path.Combine(output, ApplyArtifactNames.RuntimePrefix));
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, ApplyArtifactNames.RuntimeEtf)))
            throw new InvalidDataException(ApplyDiagnostics.Runtime);
        foreach (string name in new[] { ApplyNames.MainSource, ApplyNames.LocalSource })
            await File.WriteAllBytesAsync(Path.Combine(output, name), await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, name)));
        ApplyProcessResult compile = await Execute(
            erlc,
            [ApplyCliNames.WarningsAsErrors, ApplyCliNames.DebugInfo, ApplyCliNames.Output, output, Path.Combine(output, ApplyNames.MainSource), Path.Combine(output, ApplyNames.LocalSource)],
            Path.Combine(output, ApplyArtifactNames.CompilePrefix)
        );
        if (!Complete(compile))
            throw new InvalidDataException(ApplyDiagnostics.Compile);
        foreach (string module in new[] { ApplyNames.MainModule, ApplyNames.LocalModule })
        {
            string disassembly = ApplyModuleSource.Disassembly(output, module);
            if (!Complete(await Execute(
                erl,
                [ApplyCliNames.NoShell, ApplyCliNames.Evaluate, disassembly],
                Path.Combine(output, module + ApplyArtifactNames.DisassembleSuffix)
            )))
                throw new InvalidDataException(ApplyDiagnostics.Compile);
        }
        var rows = new List<ApplyObservation>();
        foreach (string name in scenarios)
        {
            string directory = Path.Combine(output, name);
            Directory.CreateDirectory(directory);
            string evaluation = ApplyModuleSource.Evaluation(name, capturedInput, directory);
            ApplyProcessResult observed = await Execute(
                erl,
                [ApplyCliNames.NoShell, ApplyCliNames.CodePath, output, ApplyCliNames.Evaluate, evaluation],
                Path.Combine(directory, ApplyArtifactNames.ProcessPrefix)
            );
            string? stage = File.Exists(Path.Combine(directory, ApplyArtifactNames.Stage)) ? await File.ReadAllTextAsync(Path.Combine(directory, ApplyArtifactNames.Stage)) : null;
            bool captured = Complete(observed) && stage == ApplyObservationProtocol.CompleteStage && File.Exists(Path.Combine(directory, ApplyArtifactNames.ActualEtf)) && File.Exists(Path.Combine(directory, ApplyArtifactNames.MessagesEtf));
            rows.Add(new(
                name,
                observed,
                stage,
                captured,
                await Files(directory)
            ));
            await Report(
                output,
                proof,
                declarations,
                rows,
                false
            );
        }
        await Report(
            output,
            proof,
            declarations,
            rows,
            true
        );
        if (rows.Any(row => !row.Captured))
            throw new InvalidDataException(ApplyDiagnostics.Capture);
    }

    private static bool Complete(ApplyProcessResult result) => result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static async Task<ApplyProcessResult> Execute(string executable, string[] arguments, string prefix)
    {
        await File.WriteAllTextAsync(prefix + ApplyArtifactNames.ArgumentsSuffix, JsonSerializer.Serialize(arguments));

        return await ApplyProcess.Run(executable, arguments, prefix);
    }

    private static async Task<object[]> Files(string directory, string? excludedReport = null)
    {
        var files = new List<object>();
        foreach (string path in Directory.EnumerateFiles(directory).Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(path);
            if (excludedReport is not null && (name == excludedReport || name == excludedReport + ApplyArtifactNames.TemporarySuffix))
                continue;
            byte[] bytes = await File.ReadAllBytesAsync(path);
            files.Add(new { Name = Path.GetFileName(path), Length = bytes.Length, Sha256 = Hash(bytes) });
        }

        return files.ToArray();
    }

    private static async Task Report(
        string output,
        JsonElement proof,
        byte[] declarations,
        List<ApplyObservation> rows,
        bool complete
    )
    {
        var report = new
        {
            Status = ApplyObservationProtocol.Status,
            SourceCommit = ApplyNames.Commit,
            SourceTag = ApplyNames.Tag,
            BuildEvidence = proof,
            DeclarationSha256 = Hash(declarations),
            Complete = complete,
            ExpectedExecutions = ApplyNames.ScenarioCount,
            ActualExecutions = rows.Count,
            Captured = rows.Count(row => row.Captured),
            Results = rows,
            Files = await Files(output, ApplyNames.Report),
            ReportExcludedFromInventory = true,
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false
        };
        string target = Path.Combine(output, ApplyNames.Report);
        await File.WriteAllTextAsync(
            target + ApplyArtifactNames.TemporarySuffix,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + ApplyObservationProtocol.NewLine
        );
        File.Move(target + ApplyArtifactNames.TemporarySuffix, target, true);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}

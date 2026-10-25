using System.Security.Cryptography;
using System.Text.Json;

namespace LoaderIodataContrasts;

internal static class LoaderExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != LoaderContrastNames.ArgumentCount)
            throw new ArgumentException(LoaderContrastDiagnostics.Usage);
        string root = Path.GetFullPath(arguments[0]);
        string repository = Path.GetFullPath(arguments[1]);
        string output = Path.GetFullPath(arguments[2]);
        string evidencePath = Path.GetFullPath(arguments[3]);
        using var evidence = JsonDocument.Parse(await File.ReadAllBytesAsync(evidencePath));
        JsonElement proof = evidence.RootElement;
        if (proof.GetProperty(LoaderEvidenceFields.SourceCommit).GetString() != LoaderContrastNames.Commit ||
            proof.GetProperty(LoaderEvidenceFields.SourceTag).GetString() != LoaderContrastNames.Tag ||
            !proof.GetProperty(LoaderEvidenceFields.SourceBuildCompleted).GetBoolean() ||
            Path.GetFullPath(proof.GetProperty(LoaderEvidenceFields.InstalledOtpRoot).GetString()!) != root)
            throw new InvalidDataException(LoaderContrastDiagnostics.PinnedBuildRequired);
        string buildLog = proof.GetProperty(LoaderEvidenceFields.SourceBuildLog).GetString()!;
        if (Hash(await File.ReadAllBytesAsync(buildLog)) != proof.GetProperty(LoaderEvidenceFields.SourceBuildLogSha256).GetString())
            throw new InvalidDataException(LoaderContrastDiagnostics.BuildLogChanged);
        if ((await File.ReadAllTextAsync(Path.Combine(
            root,
            LoaderArtifactPaths.ReleasesDirectory,
            LoaderContrastNames.Release,
            LoaderArtifactPaths.InstalledVersionFile
        ))).Trim() != LoaderContrastNames.Version)
            throw new InvalidDataException(LoaderContrastDiagnostics.InstalledVersionDiffers);
        string suffix = OperatingSystem.IsWindows() ? LoaderArtifactPaths.WindowsExecutableSuffix : LoaderArtifactPaths.OtherExecutableSuffix;
        string erl = Path.Combine(root, LoaderArtifactPaths.NativeExecutableDirectory, LoaderArtifactPaths.RuntimeExecutable + suffix);
        string erlc = Path.Combine(root, LoaderArtifactPaths.NativeExecutableDirectory, LoaderArtifactPaths.CompilerExecutable + suffix);
        if (!File.Exists(erl) || !File.Exists(erlc))
            throw new FileNotFoundException(LoaderContrastDiagnostics.NativeExecutablesRequired);
        if (Directory.Exists(output))
            throw new IOException(LoaderContrastDiagnostics.NewOutputRequired);
        Directory.CreateDirectory(output);
        byte[] declarations = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, LoaderContrastNames.Declarations));
        await File.WriteAllBytesAsync(Path.Combine(output, LoaderContrastNames.Declarations), declarations);
        await File.WriteAllBytesAsync(Path.Combine(output, LoaderArtifactPaths.CapturedBuildEvidence), await File.ReadAllBytesAsync(evidencePath));
        await File.WriteAllBytesAsync(Path.Combine(output, LoaderArtifactPaths.CapturedBuildLog), await File.ReadAllBytesAsync(buildLog));
        await File.WriteAllBytesAsync(
            Path.Combine(output, LoaderArtifactPaths.PinnedSourceProvenance),
            await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, LoaderArtifactPaths.PinnedSourceProvenance))
        );
        using var declared = JsonDocument.Parse(declarations);
        JsonElement[] scenarios = declared.RootElement.GetProperty(LoaderEvidenceFields.Scenarios).EnumerateArray().ToArray();
        if (scenarios.Length != LoaderContrastNames.CaseCount)
            throw new InvalidDataException(LoaderContrastDiagnostics.SixteenScenariosRequired);
        foreach (JsonElement input in declared.RootElement.GetProperty(LoaderEvidenceFields.InputBytes).EnumerateArray())
        {
            string path = input.GetProperty(LoaderEvidenceFields.Path).GetString()!;
            byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(repository, path));
            if (Hash(bytes) != input.GetProperty(LoaderEvidenceFields.Sha256).GetString())
                throw new InvalidDataException(LoaderContrastDiagnostics.ImmutableInputChangedPrefix + path);
            string captured = Path.Combine(output, LoaderArtifactPaths.InputsDirectory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(captured)!);
            await File.WriteAllBytesAsync(captured, bytes);
        }
        string versionText = Path.Combine(output, LoaderArtifactPaths.InvokedRuntimeText);
        string versionEtf = Path.Combine(output, LoaderArtifactPaths.InvokedRuntimeEtf);
        string versionExpression = LoaderErlangProtocol.RuntimeInspectionPrefix +
            LoaderErlangProtocol.RuntimeFileWritePrefix + Quote(versionEtf) + LoaderErlangProtocol.RuntimeBinaryToTextBridge + Quote(versionText) + LoaderErlangProtocol.RuntimeTextCaptureSuffix +
            LoaderErlangProtocol.RuntimeAssertionTuplePrefix + Quote(LoaderContrastNames.Release) + LoaderErlangProtocol.RuntimeAssertionSeparator + Quote(LoaderContrastNames.ErtsVersion) + LoaderErlangProtocol.RuntimeAssertionJitFields + Quote(root) + LoaderErlangProtocol.RuntimeAssertionHaltSuffix;
        await File.WriteAllTextAsync(Path.Combine(output, LoaderArtifactPaths.RuntimeEvaluation), versionExpression + LoaderCaptureText.RecordTerminator);
        LoaderProcessResult version = await LoaderProcess.Run(
            erl,
            [LoaderNativeArguments.NoShell, LoaderNativeArguments.Evaluate, versionExpression],
            Path.Combine(output, LoaderArtifactPaths.RuntimeCapturePrefix)
        );
        if (version.ExitCode != 0 || version.TimedOut || !version.RawComplete || !File.Exists(versionEtf) || !File.Exists(versionText))
            throw new InvalidDataException(LoaderContrastDiagnostics.InvokedRuntimeDiffers);
        string sourcePath = Path.Combine(output, LoaderContrastNames.Source);
        await File.WriteAllBytesAsync(sourcePath, await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, LoaderContrastNames.Source)));
        LoaderProcessResult compile = await LoaderProcess.Run(
            erlc,
            [LoaderNativeArguments.WarningsAsErrors, LoaderNativeArguments.DebugInfo, LoaderNativeArguments.OutputDirectory, output, sourcePath],
            Path.Combine(output, LoaderArtifactPaths.CompileCapturePrefix)
        );
        if (compile.ExitCode != 0 || compile.TimedOut || !compile.RawComplete)
            throw new InvalidDataException(LoaderContrastDiagnostics.WrapperCompileUnavailable);
        string wrapperBeam = Path.Combine(output, LoaderContrastNames.Wrapper + LoaderArtifactPaths.BeamFileSuffix);
        string disassembly = Path.Combine(output, LoaderArtifactPaths.DisassemblyText);
        string disassemble = LoaderErlangProtocol.DisassemblyPrefix + Quote(wrapperBeam) + LoaderErlangProtocol.DisassemblyFileBridge + Quote(disassembly) + LoaderErlangProtocol.DisassemblyTextSuffix +
            LoaderErlangProtocol.DisassemblyShapeAssertion;
        await File.WriteAllTextAsync(Path.Combine(output, LoaderArtifactPaths.DisassemblyEvaluation), disassemble + LoaderCaptureText.RecordTerminator);
        LoaderProcessResult disasm = await LoaderProcess.Run(
            erl,
            [LoaderNativeArguments.NoShell, LoaderNativeArguments.Evaluate, disassemble],
            Path.Combine(output, LoaderArtifactPaths.DisassemblyCapturePrefix)
        );
        if (disasm.ExitCode != 0 || disasm.TimedOut || !disasm.RawComplete || !File.Exists(disassembly))
            throw new InvalidDataException(LoaderContrastDiagnostics.WrapperDisassemblyMissing);
        var results = new List<LoaderCaseResult>();
        foreach (JsonElement scenario in scenarios)
        {
            string name = scenario.GetProperty(LoaderEvidenceFields.Name).GetString()!;
            string[] variants = [name];
            foreach (string variant in variants)
            {
                string directory = Path.Combine(output, variant);
                Directory.CreateDirectory(directory);
                string evaluation = LoaderContrastNames.Wrapper + LoaderErlangProtocol.CaseCallPrefix + Quote(variant) + LoaderErlangProtocol.CaseArgumentSeparator + Quote(Path.Combine(output, LoaderArtifactPaths.InputsDirectory)) + LoaderErlangProtocol.CaseArgumentSeparator + Quote(directory) + LoaderErlangProtocol.CaseCallSuffix;
                await File.WriteAllTextAsync(Path.Combine(directory, LoaderArtifactPaths.CaseEvaluation), evaluation + LoaderCaptureText.RecordTerminator);
                await File.WriteAllTextAsync(Path.Combine(directory, LoaderArtifactPaths.CaseStage), LoaderExecutionStages.Created);
                LoaderProcessResult observed = await LoaderProcess.Run(
                    erl,
                    [LoaderNativeArguments.NoShell, LoaderNativeArguments.CodePath, output, LoaderNativeArguments.Evaluate, evaluation],
                    Path.Combine(directory, LoaderArtifactPaths.CaseProcessPrefix)
                );
                string? stage = File.Exists(Path.Combine(directory, LoaderArtifactPaths.CaseStage)) ? await File.ReadAllTextAsync(Path.Combine(directory, LoaderArtifactPaths.CaseStage)) : null;
                byte[] actual = File.Exists(Path.Combine(directory, LoaderArtifactPaths.CaseActualEtf)) ? await File.ReadAllBytesAsync(Path.Combine(directory, LoaderArtifactPaths.CaseActualEtf)) : [];
                byte[] expected = File.Exists(Path.Combine(directory, LoaderArtifactPaths.CaseExpectedEtf)) ? await File.ReadAllBytesAsync(Path.Combine(directory, LoaderArtifactPaths.CaseExpectedEtf)) : [];
                bool passed = observed.ExitCode == 0 && !observed.TimedOut && observed.RawComplete && stage == LoaderExecutionStages.Completed && File.Exists(Path.Combine(directory, LoaderArtifactPaths.CaseActualEtf)) && File.Exists(Path.Combine(directory, LoaderArtifactPaths.CaseExpectedEtf)) &&
                    actual.SequenceEqual(expected);
                results.Add(new(
                    variant,
                    name,
                    observed.ExitCode,
                    observed.TimedOut,
                    observed.RawComplete,
                    stage,
                    passed,
                    await Files(directory)
                ));
                await Report(
                    output,
                    proof,
                    declarations,
                    sourcePath,
                    wrapperBeam,
                    results,
                    false
                );
            }
        }
        await Report(
            output,
            proof,
            declarations,
            sourcePath,
            wrapperBeam,
            results,
            true
        );
        if (results.Any(result => !result.Passed))
            throw new InvalidDataException(LoaderContrastDiagnostics.NativeContrastsFailed);
    }

    private static async Task Report(
        string output,
        JsonElement proof,
        byte[] declarations,
        string source,
        string beam,
        List<LoaderCaseResult> rows,
        bool complete
    )
    {
        string target = Path.Combine(output, LoaderContrastNames.Report);
        string temporary = target + LoaderArtifactPaths.TemporaryReportSuffix;
        var report = new
        {
            SourceCommit = LoaderContrastNames.Commit,
            SourceTag = LoaderContrastNames.Tag,
            BuildEvidence = proof,
            PredeclaredSha256 = Hash(declarations),
            WrapperSourceSha256 = Hash(await File.ReadAllBytesAsync(source)),
            WrapperBeamSha256 = Hash(await File.ReadAllBytesAsync(beam)),
            CaseCount = LoaderContrastNames.CaseCount,
            FreshVmExecutions = rows.Count,
            ExpectedFreshVmExecutions = LoaderContrastNames.CaseCount,
            Complete = complete,
            Passed = rows.Count(row => row.Passed),
            Results = rows,
            MfaRegistrations = 0,
            SdkExecuted = false,
            NativePhysicalCompatibilityClaim = false
        };
        await File.WriteAllTextAsync(
            temporary,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + LoaderCaptureText.RecordTerminator
        );
        File.Move(temporary, target, overwrite: true);
    }

    private static async Task<IReadOnlyList<object>> Files(string directory)
    {
        var files = new List<object>();
        foreach (string path in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            byte[] bytes = await File.ReadAllBytesAsync(path);
            files.Add(new { Name = Path.GetFileName(path), Length = bytes.Length, Sha256 = Hash(bytes) });
        }

        return files;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string Quote(string value) => LoaderErlangProtocol.Quote +
        value.Replace(LoaderErlangProtocol.Backslash, LoaderErlangProtocol.EscapedBackslash)
            .Replace(LoaderErlangProtocol.Quote, LoaderErlangProtocol.EscapedQuote)
            .Replace(LoaderErlangProtocol.CarriageReturn, LoaderErlangProtocol.EscapedCarriageReturn)
            .Replace(LoaderErlangProtocol.LineFeed, LoaderErlangProtocol.EscapedLineFeed) +
        LoaderErlangProtocol.Quote;
}

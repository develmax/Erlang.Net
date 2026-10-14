using NodeIdentityContrasts;
using System.Security.Cryptography;
using System.Text.Json;

namespace CreationBoundaryContrasts;

internal static class CreationExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != CreationNames.ArgumentCount)
            throw new ArgumentException(CreationNames.Usage);
        string root = Path.GetFullPath(arguments[0]);
        string output = Path.GetFullPath(arguments[1]);
        byte[] evidence = await File.ReadAllBytesAsync(arguments[2]);
        using var document = JsonDocument.Parse(evidence);
        JsonElement proof = document.RootElement;
        if (proof.GetProperty(NodeFields.SourceCommit).GetString() != NodeNames.Pin ||
            proof.GetProperty(NodeFields.SourceTag).GetString() != NodeNames.Tag ||
            !proof.GetProperty(NodeFields.SourceBuildCompleted).GetBoolean() ||
            Path.GetFullPath(proof.GetProperty(NodeFields.InstalledOtpRoot).GetString()!) != root)
            throw new InvalidDataException(NodeDiagnostics.Evidence);
        byte[] log = await File.ReadAllBytesAsync(proof.GetProperty(NodeFields.SourceBuildLog).GetString()!);
        string version = (await File.ReadAllTextAsync(Path.Combine(
            root,
            NodeNames.Releases,
            NodeNames.Release,
            NodeNames.VersionFile
        ))).Trim();
        if (Hash(log) != proof.GetProperty(NodeFields.SourceBuildLogSha256).GetString() || version != NodeNames.Version)
            throw new InvalidDataException(NodeDiagnostics.Evidence);
        byte[] plan = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, NodeNames.Plan));
        using var declaration = JsonDocument.Parse(plan);
        JsonElement declared = declaration.RootElement;
        if (Hash(plan) != CreationNames.PlanSha256 || declared.GetProperty(NodeFields.SourceTag).GetString() != NodeNames.Tag ||
            declared.GetProperty(NodeFields.Pin).GetString() != NodeNames.Pin ||
            !declared.GetProperty(NodeFields.Scenarios).EnumerateArray().Select(value => value.GetString()).SequenceEqual(CreationNames.Scenarios))
            throw new InvalidDataException(CreationNames.InvalidSource);
        if (!declared.GetProperty(CreationPlanFields.FreshVmPerScenario).GetBoolean() ||
            declared.GetProperty(CreationPlanFields.FreshScenarioVms).GetInt32() != CreationNames.FreshScenarioVms)
            throw new InvalidDataException(CreationNames.InvalidSource);
        var declaredSources = declared.GetProperty(CreationNames.Sources).EnumerateArray().ToDictionary(
            value => value.GetProperty(CreationNames.File).GetString()!,
            value => value.GetProperty(CreationNames.Sha256).GetString()!
        );
        if (declaredSources.Count != CreationNames.SourceHashes.Count)
            throw new InvalidDataException(CreationNames.InvalidSource);
        var sources = new Dictionary<string, byte[]>();
        foreach (var source in CreationNames.SourceHashes)
        {
            byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, source.Key));
            if (Hash(bytes) != source.Value || declaredSources.GetValueOrDefault(source.Key) != source.Value)
                throw new InvalidDataException(CreationNames.InvalidSource);
            sources.Add(source.Key, bytes);
        }
        foreach (string scenario in CreationNames.Scenarios)
            if (!declared.GetProperty(CreationNames.Required).GetProperty(scenario).EnumerateArray().Select(value => value.GetString()).SequenceEqual(CreationNames.ArtifactFiles))
                throw new InvalidDataException(CreationNames.InvalidSource);
        if (Directory.Exists(output) || File.Exists(output))
            throw new IOException(CreationNames.OutputExists);
        string suffix = OperatingSystem.IsWindows() ? NodeNames.ExecutableSuffix : string.Empty;
        string erl = Path.Combine(root, NodeNames.Bin, NodeNames.CliErl + suffix);
        string erlc = Path.Combine(root, NodeNames.Bin, NodeNames.CliErlc + suffix);
        if (!File.Exists(erl) || !File.Exists(erlc))
            throw new InvalidDataException(NodeDiagnostics.Evidence);
        Directory.CreateDirectory(output);
        foreach (var source in sources)
            await File.WriteAllBytesAsync(Path.Combine(output, source.Key), source.Value);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.Plan), plan);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildLog), log);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildEvidence), evidence);
        NodeProcessResult runtime = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliEval, NodeModuleSource.Runtime(output, root)],
            Path.Combine(output, NodeNames.RuntimePrefix)
        );
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, NodeNames.Runtime)))
            throw new InvalidDataException(NodeDiagnostics.Runtime);
        string[] compileArguments = new[] { NodeNames.CliWarnings, NodeNames.CliDebug, NodeNames.CliOutput, output }.Concat(sources.Keys.Select(name => Path.Combine(output, name))).ToArray();
        NodeProcessResult compile = await NodeProcess.Run(erlc, compileArguments, Path.Combine(output, NodeNames.CompilePrefix));
        if (!Complete(compile) || sources.Keys.Any(name => !File.Exists(Path.Combine(output, Path.GetFileNameWithoutExtension(name) + NodeNames.BinarySuffix))))
            throw new InvalidDataException(NodeDiagnostics.Compile);
        var rows = new List<CreationRow>();
        for (int index = 0; index < CreationNames.Scenarios.Length; index++)
        {
            string scenario = CreationNames.Scenarios[index];
            string directory = Path.Combine(output, scenario);
            Directory.CreateDirectory(directory);
            string invocation = CreationEvaluation.Run(scenario, directory);
            await File.WriteAllTextAsync(Path.Combine(directory, NodeNames.Evaluation), invocation);
            try
            {
                NodeProcessResult process = await NodeProcess.Run(
                    erl,
                    [NodeNames.CliNoShell, NodeNames.CliCodePath, output, NodeNames.CliEval, invocation],
                    Path.Combine(directory, NodeNames.NativePrefix)
                );
                string? stage = File.Exists(Path.Combine(directory, CreationNames.Stage)) ? await File.ReadAllTextAsync(Path.Combine(directory, CreationNames.Stage)) : null;
                bool captured = Complete(process) && stage == CreationNames.Complete && CreationNames.ArtifactFiles.All(name => File.Exists(Path.Combine(directory, name)));
                rows.Add(new(
                    scenario,
                    process.ExitCode,
                    process.TimedOut,
                    process.RawComplete,
                    stage,
                    captured,
                    null
                ));
            }
            catch (Exception exception)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, CreationNames.CollectorExceptionFile), exception.ToString());
                string? stage = File.Exists(Path.Combine(directory, CreationNames.Stage)) ? await File.ReadAllTextAsync(Path.Combine(directory, CreationNames.Stage)) : null;
                rows.Add(new(
                    scenario,
                    null,
                    null,
                    null,
                    stage,
                    false,
                    exception.GetType().FullName
                ));
            }
        }
        var report = new
        {
            Pin = NodeNames.Pin,
            Sources = CreationNames.SourceHashes,
            Planned = CreationNames.Scenarios.Length,
            FreshScenarioVMs = CreationNames.FreshScenarioVms,
            Captured = rows.Count(row => row.Captured),
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false,
            NativeNetworkingUsed = false,
            Rows = rows,
            Files = Directory.EnumerateFiles(output, NodeNames.AllFiles, SearchOption.AllDirectories).Select(path => new
            {
                Path = Path.GetRelativePath(output, path),
                Bytes = new FileInfo(path).Length,
                Sha256 = Hash(File.ReadAllBytes(path))
            }).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, CreationNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (rows.Any(row => !row.Captured))
            throw new InvalidDataException(CreationNames.CaptureFailure);
    }

    private static bool Complete(NodeProcessResult result) => result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

using NodeIdentityContrasts;
using System.Security.Cryptography;
using System.Text.Json;

namespace DistributionQueueContrasts;

internal static class QueueExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != QueueNames.ArgumentCount)
            throw new ArgumentException(QueueNames.Usage);
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
        if (Hash(plan) != QueueNames.PlanSha256 || declared.GetProperty(NodeFields.SourceTag).GetString() != NodeNames.Tag ||
            declared.GetProperty(NodeFields.Pin).GetString() != NodeNames.Pin ||
            !declared.GetProperty(NodeFields.Scenarios).EnumerateArray().Select(value => value.GetString()).SequenceEqual(QueueNames.Scenarios))
            throw new InvalidDataException(QueueNames.InvalidSource);
        if (!declared.GetProperty(QueuePlanFields.FreshVmPerScenario).GetBoolean() ||
            declared.GetProperty(QueuePlanFields.FreshScenarioVms).GetInt32() != QueueNames.FreshScenarioVms)
            throw new InvalidDataException(QueueNames.InvalidSource);
        var declaredSources = declared.GetProperty(QueueNames.Sources).EnumerateArray().ToDictionary(
            value => value.GetProperty(QueueNames.File).GetString()!,
            value => value.GetProperty(QueueNames.Sha256).GetString()!
        );
        if (declaredSources.Count != QueueNames.SourceHashes.Count)
            throw new InvalidDataException(QueueNames.InvalidSource);
        var sources = new Dictionary<string, byte[]>();
        foreach (var source in QueueNames.SourceHashes)
        {
            byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, source.Key));
            if (Hash(bytes) != source.Value || declaredSources.GetValueOrDefault(source.Key) != source.Value)
                throw new InvalidDataException(QueueNames.InvalidSource);
            sources.Add(source.Key, bytes);
        }
        foreach (string scenario in QueueNames.Scenarios)
            if (!declared.GetProperty(QueueNames.Required).GetProperty(scenario).EnumerateArray().Select(value => value.GetString()).SequenceEqual(QueueNames.RequiredArtifacts[scenario]))
                throw new InvalidDataException(QueueNames.InvalidSource);
        foreach (string scenario in QueueNames.Scenarios)
            if (declared.GetProperty(QueuePlanFields.ScenarioModules).GetProperty(scenario).GetString() != QueueNames.ScenarioModules[scenario])
                throw new InvalidDataException(QueueNames.InvalidSource);
        if (Directory.Exists(output) || File.Exists(output))
            throw new IOException(QueueNames.OutputExists);
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
        string[] compileArguments = new[] { NodeNames.CliWarnings, NodeNames.CliDebug, NodeNames.CliOutput, output }.Concat(sources.Keys.Where(name => name.EndsWith(QueueNames.SourceExtension, StringComparison.Ordinal)).Select(name => Path.Combine(output, name))).ToArray();
        NodeProcessResult compile = await NodeProcess.Run(erlc, compileArguments, Path.Combine(output, NodeNames.CompilePrefix));
        if (!Complete(compile) || sources.Keys.Where(name => name.EndsWith(QueueNames.SourceExtension, StringComparison.Ordinal)).Any(name => !File.Exists(Path.Combine(output, Path.GetFileNameWithoutExtension(name) + NodeNames.BinarySuffix))))
            throw new InvalidDataException(NodeDiagnostics.Compile);
        var rows = new List<QueueRow>();
        for (int index = 0; index < QueueNames.Scenarios.Length; index++)
        {
            string scenario = QueueNames.Scenarios[index];
            string directory = Path.Combine(output, scenario);
            Directory.CreateDirectory(directory);
            string invocation = QueueEvaluation.Run(scenario, directory);
            await File.WriteAllTextAsync(Path.Combine(directory, NodeNames.Evaluation), invocation);
            try
            {
                NodeProcessResult process = await NodeProcess.Run(
                    erl,
                    [NodeNames.CliNoShell, NodeNames.CliCodePath, output, NodeNames.CliEval, invocation],
                    Path.Combine(directory, NodeNames.NativePrefix)
                );
                string? stage = File.Exists(Path.Combine(directory, QueueNames.Stage)) ? await File.ReadAllTextAsync(Path.Combine(directory, QueueNames.Stage)) : null;
                bool captured = Complete(process) && stage == QueueNames.Complete && QueueNames.RequiredArtifacts[scenario].All(name => File.Exists(Path.Combine(directory, name)));
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
                await File.WriteAllTextAsync(Path.Combine(directory, QueueNames.CollectorExceptionFile), exception.ToString());
                string? stage = File.Exists(Path.Combine(directory, QueueNames.Stage)) ? await File.ReadAllTextAsync(Path.Combine(directory, QueueNames.Stage)) : null;
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
            Sources = QueueNames.SourceHashes,
            Planned = QueueNames.Scenarios.Length,
            FreshScenarioVMs = QueueNames.FreshScenarioVms,
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
            Path.Combine(output, QueueNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (rows.Any(row => !row.Captured))
            throw new InvalidDataException(QueueNames.CaptureFailure);
    }

    private static bool Complete(NodeProcessResult result) => result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

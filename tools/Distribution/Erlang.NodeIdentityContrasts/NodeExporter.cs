using System.Security.Cryptography;
using System.Text.Json;

namespace NodeIdentityContrasts;

internal static class NodeExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != NodeNames.Arguments)
            throw new ArgumentException(NodeDiagnostics.Usage);
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
        byte[] source = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, NodeNames.Source));
        byte[] pendingSource = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, NodeNames.PendingSource));
        byte[] plan = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, NodeNames.Plan));
        using var declaration = JsonDocument.Parse(plan);
        string[] scenarios = [NodeNames.Start, NodeNames.Stop, NodeNames.Restart];
        JsonElement declared = declaration.RootElement;
        if (Hash(source) != NodeNames.SourceSha256 ||
            declared.GetProperty(NodeFields.SourceSha256).GetString() != NodeNames.SourceSha256 ||
            Hash(pendingSource) != NodeNames.PendingSourceSha256 ||
            declared.GetProperty(NodeFields.PendingSourceSha256).GetString() != NodeNames.PendingSourceSha256 ||
            declared.GetProperty(NodeFields.PendingScenario).GetString() != NodeNames.PendingScenario ||
            declared.GetProperty(NodeFields.Pin).GetString() != NodeNames.Pin ||
            !declared.GetProperty(NodeFields.Scenarios).EnumerateArray().Select(item => item.GetString()).SequenceEqual(scenarios))
            throw new InvalidDataException(NodeDiagnostics.Source);
        if (Directory.Exists(output))
            throw new IOException(NodeDiagnostics.FreshOutput);
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.Source), source);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.PendingSource), pendingSource);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.Plan), plan);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildLog), log);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildEvidence), evidence);
        string suffix = OperatingSystem.IsWindows() ? NodeNames.ExecutableSuffix : string.Empty;
        string erl = Path.Combine(root, NodeNames.Bin, NodeNames.CliErl + suffix);
        string erlc = Path.Combine(root, NodeNames.Bin, NodeNames.CliErlc + suffix);
        string evaluation = NodeModuleSource.Runtime(output, root);
        NodeProcessResult runtime = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliEval, evaluation],
            Path.Combine(output, NodeNames.RuntimePrefix)
        );
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, NodeNames.Runtime)))
            throw new InvalidDataException(NodeDiagnostics.Runtime);
        NodeProcessResult compile = await NodeProcess.Run(
            erlc,
            [NodeNames.CliWarnings, NodeNames.CliDebug, NodeNames.CliOutput, output,
                Path.Combine(output, NodeNames.Source), Path.Combine(output, NodeNames.PendingSource)],
            Path.Combine(output, NodeNames.CompilePrefix)
        );
        if (!Complete(compile))
            throw new InvalidDataException(NodeDiagnostics.Compile);
        foreach (string module in new[] { NodeNames.Module, NodeNames.PendingModule })
        {
            NodeProcessResult disassembly = await NodeProcess.Run(
                erl,
                [NodeNames.CliNoShell, NodeNames.CliEval, NodeModuleSource.Disassembly(output, module)],
                Path.Combine(output, module + NodeNames.DisassemblyPrefix)
            );
            if (!Complete(disassembly) || !File.Exists(Path.Combine(output, module + NodeNames.Disassembly)))
                throw new InvalidDataException(NodeDiagnostics.Compile);
        }
        var rows = new List<NodeObservation>();
        foreach (string scenario in scenarios)
        {
            string directory = Path.Combine(output, scenario);
            Directory.CreateDirectory(directory);
            string invocation = NodeModuleSource.Evaluate(scenario, directory);
            await File.WriteAllTextAsync(Path.Combine(directory, NodeNames.Evaluation), invocation);
            NodeProcessResult process = await NodeProcess.Run(
                erl,
                [NodeNames.CliNoShell, NodeNames.CliCodePath, output, NodeNames.CliEval, invocation],
                Path.Combine(directory, NodeNames.NativePrefix)
            );
            string? stage = File.Exists(Path.Combine(directory, NodeNames.Stage))
                ? await File.ReadAllTextAsync(Path.Combine(directory, NodeNames.Stage))
                : null;
            string[] phases = scenario == NodeNames.Start
                ? [NodeNames.Before, NodeNames.Started]
                : scenario == NodeNames.Stop
                    ? [NodeNames.Before, NodeNames.Started, NodeNames.Stopped]
                    : [NodeNames.Before, NodeNames.Started, NodeNames.Stopped, NodeNames.Restarted];
            bool captured = Complete(process) && stage == NodeNames.CompleteStage &&
                new[] { NodeNames.Inputs, NodeNames.CleanupKernel, NodeNames.CleanupChild }
                    .All(name => File.Exists(Path.Combine(directory, name))) &&
                phases.All(phase => new[] { NodeNames.StandardSuffix, NodeNames.LocalSuffix, NodeNames.FactsSuffix }
                    .All(ending => File.Exists(Path.Combine(directory, phase + ending)))) &&
                phases.Skip(1).All(phase => File.Exists(Path.Combine(directory, phase + NodeNames.OperationSuffix)));
            rows.Add(new(
                scenario,
                process.ExitCode,
                process.TimedOut,
                process.RawComplete,
                stage,
                captured
            ));
        }
        string pendingDirectory = Path.Combine(output, NodeNames.PendingScenario);
        Directory.CreateDirectory(pendingDirectory);
        string pendingEvaluation = NodePendingSource.Evaluate(pendingDirectory);
        await File.WriteAllTextAsync(Path.Combine(pendingDirectory, NodeNames.Evaluation), pendingEvaluation);
        NodeProcessResult pending = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliCodePath, output, NodeNames.CliEval, pendingEvaluation],
            Path.Combine(pendingDirectory, NodeNames.NativePrefix)
        );
        string? pendingStage = File.Exists(Path.Combine(pendingDirectory, NodeNames.Stage))
            ? await File.ReadAllTextAsync(Path.Combine(pendingDirectory, NodeNames.Stage))
            : null;
        rows.Add(new(
            NodeNames.PendingScenario,
            pending.ExitCode,
            pending.TimedOut,
            pending.RawComplete,
            pendingStage,
            Complete(pending) && pendingStage == NodeNames.CompleteStage &&
                File.Exists(Path.Combine(pendingDirectory, NodeNames.PendingActual))
        ));
        var report = new
        {
            Pin = NodeNames.Pin,
            SourceSha256 = Hash(source),
            PendingSourceSha256 = Hash(pendingSource),
            Captured = rows.Count(row => row.Captured),
            Planned = NodeNames.NativeExecutions,
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false,
            IndependentPendingSourceAssertionsCompleted = rows.Last().Captured,
            Rows = rows,
            Files = Directory.EnumerateFiles(output, NodeNames.AllFiles, SearchOption.AllDirectories)
                .Select(path => new
                {
                    Path = Path.GetRelativePath(output, path),
                    Length = new FileInfo(path).Length,
                    Sha256 = Hash(File.ReadAllBytes(path))
                }).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, NodeNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (rows.Any(row => !row.Captured))
            throw new InvalidDataException(NodeDiagnostics.Capture);
    }

    private static bool Complete(NodeProcessResult result) =>
        result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

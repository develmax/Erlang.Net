using NodeIdentityContrasts;
using System.Security.Cryptography;
using System.Text.Json;

namespace MixedMapIdentityContrasts;

internal static class MixedExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != MixedNames.Arguments)
            throw new ArgumentException(MixedNames.Usage);
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
        byte[] source = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, MixedNames.Source));
        byte[] plan = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, NodeNames.Plan));
        using var declaration = JsonDocument.Parse(plan);
        JsonElement declared = declaration.RootElement;
        if (Hash(source) != MixedNames.SourceSha256 ||
            declared.GetProperty(NodeFields.SourceSha256).GetString() != MixedNames.SourceSha256 ||
            declared.GetProperty(NodeFields.Pin).GetString() != NodeNames.Pin ||
            !declared.GetProperty(NodeFields.Scenarios).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(new[] { MixedNames.Scenario }) ||
            !declared.GetProperty(MixedNames.RequiredArtifactsField).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(MixedNames.RequiredArtifacts))
            throw new InvalidDataException(MixedNames.InvalidPlan);
        if (Directory.Exists(output))
            throw new IOException(MixedNames.OutputExists);
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, MixedNames.Source), source);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.Plan), plan);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildLog), log);
        await File.WriteAllBytesAsync(Path.Combine(output, NodeNames.BuildEvidence), evidence);
        string suffix = OperatingSystem.IsWindows() ? NodeNames.ExecutableSuffix : string.Empty;
        string erl = Path.Combine(root, NodeNames.Bin, NodeNames.CliErl + suffix);
        string erlc = Path.Combine(root, NodeNames.Bin, NodeNames.CliErlc + suffix);
        NodeProcessResult runtime = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliEval, NodeModuleSource.Runtime(output, root)],
            Path.Combine(output, NodeNames.RuntimePrefix)
        );
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, NodeNames.Runtime)))
            throw new InvalidDataException(NodeDiagnostics.Runtime);
        NodeProcessResult compile = await NodeProcess.Run(
            erlc,
            [NodeNames.CliWarnings, NodeNames.CliDebug, NodeNames.CliOutput, output, Path.Combine(output, MixedNames.Source)],
            Path.Combine(output, NodeNames.CompilePrefix)
        );
        if (!Complete(compile) || !File.Exists(Path.Combine(output, MixedNames.Module + NodeNames.BinarySuffix)))
            throw new InvalidDataException(NodeDiagnostics.Compile);
        NodeProcessResult disassembly = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliEval, NodeModuleSource.Disassembly(output, MixedNames.Module)],
            Path.Combine(output, NodeNames.DisassemblyPrefix)
        );
        if (!Complete(disassembly) || !File.Exists(Path.Combine(output, MixedNames.Module + NodeNames.Disassembly)))
            throw new InvalidDataException(NodeDiagnostics.Compile);
        string directory = Path.Combine(output, MixedNames.Scenario);
        Directory.CreateDirectory(directory);
        string invocation = MixedModuleSource.Evaluate(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, NodeNames.Evaluation), invocation);
        NodeProcessResult process = await NodeProcess.Run(
            erl,
            [NodeNames.CliNoShell, NodeNames.CliCodePath, output, NodeNames.CliEval, invocation],
            Path.Combine(directory, NodeNames.NativePrefix)
        );
        string? stage = File.Exists(Path.Combine(directory, MixedNames.Stage))
            ? await File.ReadAllTextAsync(Path.Combine(directory, MixedNames.Stage))
            : null;
        bool captured = Complete(process) && stage == MixedNames.CompleteStage &&
            MixedNames.RequiredArtifacts.All(name => File.Exists(Path.Combine(directory, name)));
        var report = new
        {
            Pin = NodeNames.Pin,
            SourceSha256 = Hash(source),
            Captured = captured ? MixedNames.NativeExecutions : 0,
            Planned = MixedNames.NativeExecutions,
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false,
            NativeNetworkingUsed = false,
            NoSemanticAssertionsForUnobservedOutcomes = true,
            Rows = new[] { new { Name = MixedNames.Scenario, process.ExitCode, process.TimedOut, process.RawComplete, Stage = stage, Captured = captured } },
            Files = Directory.EnumerateFiles(output, NodeNames.AllFiles, SearchOption.AllDirectories)
                .Select(path => new
                {
                    Path = Path.GetRelativePath(output, path),
                    Length = new FileInfo(path).Length,
                    Sha256 = Hash(File.ReadAllBytes(path))
                }).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, MixedNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (!captured)
            throw new InvalidDataException(MixedNames.CaptureIncomplete);
    }

    private static bool Complete(NodeProcessResult result) =>
        result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

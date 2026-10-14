using System.Security.Cryptography;
using System.Text.Json;

namespace DistributionPacketContrasts;

internal static class PacketExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != PacketNames.Arguments)
            throw new ArgumentException(PacketDiagnostics.Usage);
        string root = Path.GetFullPath(arguments[0]);
        string output = Path.GetFullPath(arguments[1]);
        byte[] evidence = await File.ReadAllBytesAsync(arguments[2]);
        using var document = JsonDocument.Parse(evidence);
        JsonElement proof = document.RootElement;
        if (proof.GetProperty(PacketFields.SourceCommit).GetString() != PacketNames.Pin ||
            proof.GetProperty(PacketFields.SourceTag).GetString() != PacketNames.Tag ||
            !proof.GetProperty(PacketFields.SourceBuildCompleted).GetBoolean() ||
            Path.GetFullPath(proof.GetProperty(PacketFields.InstalledOtpRoot).GetString()!) != root)
            throw new InvalidDataException(PacketDiagnostics.Evidence);
        byte[] log = await File.ReadAllBytesAsync(proof.GetProperty(PacketFields.SourceBuildLog).GetString()!);
        string version = (await File.ReadAllTextAsync(Path.Combine(
            root,
            PacketNames.Releases,
            PacketNames.Release,
            PacketNames.VersionFile
        ))).Trim();
        if (Hash(log) != proof.GetProperty(PacketFields.SourceBuildLogSha256).GetString() || version != PacketNames.Version)
            throw new InvalidDataException(PacketDiagnostics.Evidence);
        byte[] source = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, PacketNames.Source));
        byte[] plan = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, PacketNames.Plan));
        using var declaration = JsonDocument.Parse(plan);
        JsonElement declared = declaration.RootElement;
        if (Hash(source) != PacketNames.SourceSha256 ||
            declared.GetProperty(PacketFields.SourceSha256).GetString() != PacketNames.SourceSha256 ||
            declared.GetProperty(PacketFields.Pin).GetString() != PacketNames.Pin ||
            !declared.GetProperty(PacketFields.Scenarios).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(new[] { PacketNames.Scenario }) ||
            !declared.GetProperty(PacketFields.RequiredArtifacts).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(PacketNames.RequiredArtifacts))
            throw new InvalidDataException(PacketDiagnostics.Source);
        if (Directory.Exists(output))
            throw new IOException(PacketDiagnostics.FreshOutput);
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.Source), source);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.Plan), plan);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.BuildLog), log);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.BuildEvidence), evidence);
        string suffix = OperatingSystem.IsWindows() ? PacketNames.ExecutableSuffix : string.Empty;
        string erl = Path.Combine(root, PacketNames.Bin, PacketNames.CliErl + suffix);
        string erlc = Path.Combine(root, PacketNames.Bin, PacketNames.CliErlc + suffix);
        PacketProcessResult runtime = await PacketProcess.Run(
            erl,
            [PacketNames.CliNoShell, PacketNames.CliEval, PacketModuleSource.Runtime(output, root)],
            Path.Combine(output, PacketNames.RuntimePrefix)
        );
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, PacketNames.Runtime)))
            throw new InvalidDataException(PacketDiagnostics.Runtime);
        PacketProcessResult compile = await PacketProcess.Run(
            erlc,
            [PacketNames.CliWarnings, PacketNames.CliDebug, PacketNames.CliOutput, output, Path.Combine(output, PacketNames.Source)],
            Path.Combine(output, PacketNames.CompilePrefix)
        );
        if (!Complete(compile) || !File.Exists(Path.Combine(output, PacketNames.Module + PacketNames.BinarySuffix)))
            throw new InvalidDataException(PacketDiagnostics.Compile);
        PacketProcessResult disassembly = await PacketProcess.Run(
            erl,
            [PacketNames.CliNoShell, PacketNames.CliEval, PacketModuleSource.Disassembly(output)],
            Path.Combine(output, PacketNames.DisassemblyPrefix)
        );
        if (!Complete(disassembly) || !File.Exists(Path.Combine(output, PacketNames.Module + PacketNames.Disassembly)))
            throw new InvalidDataException(PacketDiagnostics.Compile);
        string directory = Path.Combine(output, PacketNames.Scenario);
        Directory.CreateDirectory(directory);
        string invocation = PacketModuleSource.Evaluate(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, PacketNames.Evaluation), invocation);
        PacketProcessResult process = await PacketProcess.Run(
            erl,
            [PacketNames.CliNoShell, PacketNames.CliCodePath, output, PacketNames.CliEval, invocation],
            Path.Combine(directory, PacketNames.NativePrefix)
        );
        string? stage = File.Exists(Path.Combine(directory, PacketNames.Stage))
            ? await File.ReadAllTextAsync(Path.Combine(directory, PacketNames.Stage))
            : null;
        bool captured = Complete(process) && stage == PacketNames.CompleteStage &&
            PacketNames.RequiredArtifacts.All(name => File.Exists(Path.Combine(directory, name)));
        var report = new
        {
            Pin = PacketNames.Pin,
            SourceSha256 = Hash(source),
            Captured = captured ? PacketNames.NativeExecutions : 0,
            Planned = PacketNames.NativeExecutions,
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false,
            NativeNetworkingUsed = false,
            ProcessControllerAdoptionAttempted = true,
            Rows = new[] { new { Name = PacketNames.Scenario, process.ExitCode, process.TimedOut, process.RawComplete, Stage = stage, Captured = captured } },
            Files = Directory.EnumerateFiles(output, PacketNames.AllFiles, SearchOption.AllDirectories)
                .Select(path => new
                {
                    Path = Path.GetRelativePath(output, path),
                    Length = new FileInfo(path).Length,
                    Sha256 = Hash(File.ReadAllBytes(path))
                }).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, PacketNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (!captured)
            throw new InvalidDataException(PacketDiagnostics.Capture);
    }

    private static bool Complete(PacketProcessResult result) =>
        result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

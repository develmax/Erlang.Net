using System.Security.Cryptography;
using System.Text.Json;

using DistributionPacketContrasts;

namespace DistributionTraceObservations;

internal static class TraceExporter
{
    internal static async Task Run(string[] arguments)
    {
        if (arguments.Length != PacketNames.Arguments)
            throw new ArgumentException(TraceNames.Usage);
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
        byte[] source = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, TraceNames.Source));
        byte[] plan = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, TraceNames.Plan));
        using var declaration = JsonDocument.Parse(plan);
        JsonElement declared = declaration.RootElement;
        if (Hash(plan) != TraceNames.PlanSha256 ||
            Hash(source) != TraceNames.SourceSha256 ||
            declared.GetProperty(TraceFields.FreshVmRequiredPerCase).GetBoolean() != true ||
            declared.GetProperty(TraceFields.SemanticAssertions).GetBoolean() ||
            !declared.GetProperty(TraceFields.NoGuessedExpectedResults).GetBoolean() ||
            declared.GetProperty(TraceFields.ProbeSha256).GetString() != TraceNames.SourceSha256 ||
            declared.GetProperty(TraceFields.OtpCommit).GetString() != PacketNames.Pin ||
            !declared.GetProperty(TraceFields.Cases).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(TraceNames.Scenarios) ||
            !declared.GetProperty(TraceFields.RequiredOutputs).EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(TraceNames.RequiredArtifacts))
            throw new InvalidDataException(PacketDiagnostics.Source);
        if (Directory.Exists(output))
            throw new IOException(PacketDiagnostics.FreshOutput);
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, TraceNames.Source), source);
        await File.WriteAllBytesAsync(Path.Combine(output, TraceNames.Plan), plan);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.BuildLog), log);
        await File.WriteAllBytesAsync(Path.Combine(output, PacketNames.BuildEvidence), evidence);
        string suffix = OperatingSystem.IsWindows() ? PacketNames.ExecutableSuffix : string.Empty;
        string erl = Path.Combine(root, PacketNames.Bin, PacketNames.CliErl + suffix);
        string erlc = Path.Combine(root, PacketNames.Bin, PacketNames.CliErlc + suffix);
        PacketProcessResult runtime = await PacketProcess.Run(
            erl,
            [PacketNames.CliNoShell, PacketNames.CliEval, TraceModuleSource.Runtime(output, root)],
            Path.Combine(output, PacketNames.RuntimePrefix)
        );
        if (!Complete(runtime) || !File.Exists(Path.Combine(output, PacketNames.Runtime)))
            throw new InvalidDataException(PacketDiagnostics.Runtime);
        PacketProcessResult compile = await PacketProcess.Run(
            erlc,
            [PacketNames.CliWarnings, PacketNames.CliDebug, PacketNames.CliOutput, output, Path.Combine(output, TraceNames.Source)],
            Path.Combine(output, PacketNames.CompilePrefix)
        );
        if (!Complete(compile) || !File.Exists(Path.Combine(output, TraceNames.Module + PacketNames.BinarySuffix)))
            throw new InvalidDataException(PacketDiagnostics.Compile);
        PacketProcessResult disassembly = await PacketProcess.Run(
            erl,
            [PacketNames.CliNoShell, PacketNames.CliEval, TraceModuleSource.Disassembly(output)],
            Path.Combine(output, PacketNames.DisassemblyPrefix)
        );
        if (!Complete(disassembly) || !File.Exists(Path.Combine(output, TraceNames.Module + PacketNames.Disassembly)))
            throw new InvalidDataException(PacketDiagnostics.Compile);
        var rows = new List<TraceObservationRow>();
        foreach (string scenario in TraceNames.Scenarios)
        {
            string directory = Path.Combine(output, scenario);
            PacketProcessResult? process = null;
            string? stage = null;
            string? error = null;
            bool captured = false;
            try
            {
                Directory.CreateDirectory(directory);
                string invocation = TraceModuleSource.Evaluate(directory, scenario);
                await File.WriteAllTextAsync(Path.Combine(directory, PacketNames.Evaluation), invocation);
                process = await PacketProcess.Run(
                    erl,
                    [PacketNames.CliNoShell, PacketNames.CliCodePath, output, PacketNames.CliEval, invocation],
                    Path.Combine(directory, PacketNames.NativePrefix)
                );
                stage = File.Exists(Path.Combine(directory, TraceNames.Stage))
                    ? Convert.ToHexString(await File.ReadAllBytesAsync(Path.Combine(directory, TraceNames.Stage)))
                    : null;
                captured = Complete(process) && stage is not null &&
                    TraceNames.RequiredArtifacts.All(name => File.Exists(Path.Combine(directory, name)));
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(directory, TraceNames.ProcessError), error);
                }
                catch (Exception sidecarException)
                {
                    error += Environment.NewLine + TraceNames.ErrorSidecarFailure + sidecarException;
                }
            }
            rows.Add(new(
                scenario,
                process,
                stage,
                captured,
                error
            ));
        }
        var report = new
        {
            Pin = PacketNames.Pin,
            SourceSha256 = Hash(source),
            Captured = rows.Count(row => row.Captured),
            Planned = TraceNames.NativeExecutions,
            SdkExpectedUsed = false,
            SemanticCompatibilityClaim = false,
            NativeNetworkingUsed = false,
            ProcessControllerAdoptionAttempted = true,
            Rows = rows,
            Files = Directory.EnumerateFiles(output, PacketNames.AllFiles, SearchOption.AllDirectories)
                .Select(path => new
                {
                    Path = Path.GetRelativePath(output, path),
                    Length = new FileInfo(path).Length,
                    Sha256 = Hash(File.ReadAllBytes(path))
                }).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(output, TraceNames.Report),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        );
        if (rows.Any(row => !row.Captured))
            throw new InvalidDataException(PacketDiagnostics.Capture);
    }

    private static bool Complete(PacketProcessResult result) =>
        result.ExitCode == 0 && !result.TimedOut && result.RawComplete;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

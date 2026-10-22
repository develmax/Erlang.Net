using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Erlang.BeamBuild;

public sealed class BeamSourceBuildTask : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private readonly CancellationTokenSource stopping = new();

    [Required] public ITaskItem[] Sources { get; set; } = [];
    [Required] public string InstalledRoot { get; set; } = string.Empty;
    [Required] public string ProvenanceKind { get; set; } = string.Empty;
    [Required] public string AdmissionReceipt { get; set; } = string.Empty;
    [Required] public string AdmissionReceiptSha256 { get; set; } = string.Empty;
    [Required] public string CompilerSha256 { get; set; } = string.Empty;
    [Required] public string OutputDirectory { get; set; } = string.Empty;
    [Required] public string DotNetHost { get; set; } = string.Empty;
    [Required] public string ToolAssemblyPath { get; set; } = string.Empty;
    public string SourcePolicy { get; set; } = string.Empty;
    public string SourceRoot { get; set; } = string.Empty;
    public string RuntimeSha256 { get; set; } = string.Empty;
    public ITaskItem[] Headers { get; set; } = [];
    public ITaskItem[] IncludeDirectories { get; set; } = [];
    public ITaskItem[] Defines { get; set; } = [];
    [Output] public ITaskItem[] BeamFiles { get; private set; } = [];

    public void Cancel()
    {
        try
        {
            stopping.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // MSBuild cancellation racing final task settlement owns no remaining child.
        }
    }

    public override bool Execute()
    {
        BeamFiles = [];
        try
        {
            System.Threading.Tasks.Task.Run(ExecuteOwned).GetAwaiter().GetResult();

            return !Log.HasLoggedErrors;
        }
        catch (Exception error)
        {
            Log.LogErrorFromException(error, showStackTrace: false);

            return false;
        }
        finally
        {
            stopping.Dispose();
        }
    }

    private async System.Threading.Tasks.Task ExecuteOwned()
    {
        stopping.Token.ThrowIfCancellationRequested();
        foreach (string path in new[] { DotNetHost, ToolAssemblyPath, OutputDirectory, InstalledRoot, AdmissionReceipt })
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        StringComparison paths = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!File.Exists(DotNetHost) || !File.Exists(ToolAssemblyPath)
            || !Path.GetFullPath(ToolAssemblyPath).Equals(Path.GetFullPath(typeof(BeamSourceBuildTask).Assembly.Location), paths))
            throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        BeamBuildSource[] sources = Sources.Select(item => new BeamBuildSource(
            string.IsNullOrEmpty(item.GetMetadata(BeamSourceBuildTaskNames.Module))
                ? Path.GetFileNameWithoutExtension(item.ItemSpec) : item.GetMetadata(BeamSourceBuildTaskNames.Module),
            Path.GetFullPath(item.GetMetadata(BeamSourceBuildTaskNames.FullPath))
        )).ToArray();
        bool declared = !string.IsNullOrEmpty(SourcePolicy);
        if (!declared && (SourceRoot.Length != 0 || RuntimeSha256.Length != 0
            || Headers.Length != 0 || IncludeDirectories.Length != 0 || Defines.Length != 0))
            throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        var request = new BeamBuildRequest(
            InstalledRoot,
            ProvenanceKind,
            AdmissionReceipt,
            AdmissionReceiptSha256,
            CompilerSha256,
            OutputDirectory,
            sources,
            declared ? SourcePolicy : null,
            declared ? SourceRoot : null,
            declared ? Headers.Select(item => new BeamBuildHeader(item.GetMetadata(BeamSourceBuildTaskNames.FullPath))).ToArray() : null,
            declared ? IncludeDirectories.Select(item => item.ItemSpec).ToArray() : null,
            declared ? Defines.Select(ReadDefine).ToArray() : null,
            declared ? RuntimeSha256 : null
        );
        _ = BeamBuildRequest.Validate(request);
        Directory.CreateDirectory(OutputDirectory);
        string requestFile = BeamBuildStorage.WriteRequest(
            OutputDirectory,
            BeamSourceBuildTaskNames.RequestPrefix,
            BeamSourceBuildTaskNames.RequestExtension,
            request
        );
        _ = BeamBuildRequest.Read(requestFile);
        var start = new ProcessStartInfo(DotNetHost)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = OutputDirectory
        };
        start.ArgumentList.Add(ToolAssemblyPath);
        start.ArgumentList.Add(requestFile);
        using var process = Process.Start(start) ?? throw new IOException(BeamSourceBuildTaskNames.Failed);
        System.Threading.Tasks.Task<string> stdout = Drain(process.StandardOutput);
        System.Threading.Tasks.Task<string> stderr = Drain(process.StandardError);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(BeamSourceBuildTaskNames.BuildSeconds));
        var failures = new List<Exception>();
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (Exception primary)
        {
            failures.Add(primary);
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception failedKill)
            {
                failures.Add(failedKill);
            }
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(BeamSourceBuildTaskNames.CleanupSeconds));
            }
            catch (Exception failedWait)
            {
                failures.Add(failedWait);
            }
        }
        await LogOutput(stdout, failures);
        await LogOutput(stderr, failures);
        if (failures.Count != 0)
            throw new AggregateException(BeamSourceBuildTaskNames.Failed, failures);
        stopping.Token.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
            throw new InvalidDataException(BeamSourceBuildTaskNames.Failed);
        BeamBuildState state = JsonSerializer.Deserialize<BeamBuildState>(File.ReadAllBytes(Path.Combine(OutputDirectory, BeamBuildNames.Manifest)))
            ?? throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        if (state.Outputs.Length != sources.Length
            || state.CompilerSha256 != CompilerSha256.ToUpperInvariant()
            || state.EvidenceSha256 != AdmissionReceiptSha256.ToUpperInvariant()
            || state.ToolModuleVersionId != typeof(BeamSourceBuildTask).Module.ModuleVersionId.ToString()
            || BeamBuildStorage.HashFile(AdmissionReceipt) != state.EvidenceSha256)
            throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        var verified = new List<ITaskItem>();
        if (declared)
        {
            var currentInputs = new BeamDeclaredInputSnapshot(request);
            if (state.Preprocessing is null || state.Preprocessing.Policy != SourcePolicy
                || state.Preprocessing.RuntimeSha256 != RuntimeSha256.ToUpperInvariant()
                || state.Preprocessing.ClosureSha256 != currentInputs.ConfigurationSha256
                || !state.Preprocessing.Inputs.SequenceEqual(currentInputs.Inventory))
                throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        }
        else if (state.Preprocessing is not null)
        {
            throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
        }
        foreach (BeamBuildSource source in sources)
        {
            BeamBuildOutput value = state.Outputs.Single(row => row.Module == source.Module);
            string beam = Path.Combine(OutputDirectory, source.Module + BeamBuildNames.BeamExtension);
            if (!Path.GetFullPath(value.SourcePath).Equals(source.Path, paths)
                || BeamBuildStorage.HashFile(source.Path) != value.SourceSha256
                || BeamBuildStorage.HashFile(beam) != value.OutputSha256)
                throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);
            var item = new TaskItem(beam);
            item.SetMetadata(BeamSourceBuildTaskNames.Module, source.Module);
            item.SetMetadata(BeamSourceBuildTaskNames.Source, source.Path);
            item.SetMetadata(BeamSourceBuildTaskNames.InputSha, value.SourceSha256);
            item.SetMetadata(BeamSourceBuildTaskNames.OutputSha, value.OutputSha256);
            verified.Add(item);
        }
        stopping.Token.ThrowIfCancellationRequested();
        BeamFiles = verified.ToArray();
    }

    private static BeamBuildDefine ReadDefine(ITaskItem item)
    {
        string kind = item.GetMetadata(BeamSourceBuildTaskNames.MacroKind);
        string text = item.GetMetadata(BeamSourceBuildTaskNames.MacroText);
        if (kind.Length == 0 && text.Length != 0)
            throw new InvalidDataException(BeamSourceBuildTaskNames.Invalid);

        return new BeamBuildDefine(item.ItemSpec, kind.Length == 0 ? null : new BeamBuildDefineValue(kind, text));
    }

    private static async System.Threading.Tasks.Task<string> Drain(StreamReader reader)
    {
        var text = new StringBuilder();
        char[] buffer = new char[BeamSourceBuildTaskNames.DrainBufferCharacters];
        bool truncated = false;
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0)
        {
            int keep = Math.Min(count, BeamSourceBuildTaskNames.LogCharacters - text.Length);
            if (keep < count)
                truncated = true;
            if (keep > 0)
                text.Append(buffer, 0, keep);
        }
        if (truncated)
            text.Append(BeamSourceBuildTaskNames.Truncated);

        return text.ToString();
    }

    private async System.Threading.Tasks.Task LogOutput(
        System.Threading.Tasks.Task<string> output,
        List<Exception> failures
    )
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal,
                await output.WaitAsync(TimeSpan.FromSeconds(BeamSourceBuildTaskNames.CleanupSeconds)));
        }
        catch (Exception failure)
        {
            failures.Add(failure);
            _ = output.ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
    }
}

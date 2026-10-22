using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Erlang.BeamBuild;

public sealed class BeamApplicationBuildTask : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private readonly CancellationTokenSource stopping = new();

    [Required] public ITaskItem[] Applications { get; set; } = [];
    public ITaskItem[] Modules { get; set; } = [];
    [Required] public string InstalledRoot { get; set; } = string.Empty;
    [Required] public string ProvenanceKind { get; set; } = string.Empty;
    [Required] public string AdmissionReceipt { get; set; } = string.Empty;
    [Required] public string AdmissionReceiptSha256 { get; set; } = string.Empty;
    [Required] public string CompilerSha256 { get; set; } = string.Empty;
    [Required] public string RuntimeSha256 { get; set; } = string.Empty;
    [Required] public string SourceRoot { get; set; } = string.Empty;
    [Required] public string OutputDirectory { get; set; } = string.Empty;
    [Required] public string DotNetHost { get; set; } = string.Empty;
    [Required] public string ToolAssemblyPath { get; set; } = string.Empty;
    public ITaskItem[] Priv { get; set; } = [];
    public ITaskItem[] Configuration { get; set; } = [];
    public ITaskItem[] RawNone { get; set; } = [];
    public ITaskItem[] RawContent { get; set; } = [];
    public ITaskItem[] ComputedCopyItems { get; set; } = [];
    [Output] public ITaskItem[] AppFiles { get; private set; } = [];
    [Output] public ITaskItem[] OriginalInputs { get; private set; } = [];
    [Output] public ITaskItem[] ModulesToRelocate { get; private set; } = [];

    public void Cancel()
    {
        try
        {
            stopping.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    public override bool Execute()
    {
        AppFiles = [];
        OriginalInputs = [];
        ModulesToRelocate = [];
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
        foreach (string path in new[] { DotNetHost, ToolAssemblyPath, InstalledRoot, AdmissionReceipt, SourceRoot, OutputDirectory })
            _ = BeamApplicationTaskPaths.Absolute(path);
        StringComparison paths = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (Applications.Length != 1 || !File.Exists(DotNetHost)
            || !Path.GetFullPath(ToolAssemblyPath).Equals(typeof(BeamApplicationBuildTask).Assembly.Location, paths))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        string sourceDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(SourceRoot));
        string outputDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(OutputDirectory));
        if (sourceDirectory.Equals(outputDirectory, paths)
            || outputDirectory.StartsWith(sourceDirectory + Path.DirectorySeparatorChar, paths)
            || sourceDirectory.StartsWith(outputDirectory + Path.DirectorySeparatorChar, paths))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        string toolSha = BeamApplicationTaskPaths.Hash(ToolAssemblyPath);
        ITaskItem app = Applications[0];
        string appName = app.GetMetadata(BeamApplicationBuildTaskNames.ApplicationName);
        string version = app.GetMetadata(BeamApplicationBuildTaskNames.Version);
        if (appName.Length == 0 || version.Length == 0 || appName.Contains('/') || version.Contains('/'))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        string prefix = BeamApplicationTaskPaths.Portable(BeamApplicationBuildTaskNames.LibraryPrefix + appName + "-" + version) + "/";
        string descriptor = Full(app);
        if (!descriptor.EndsWith(BeamApplicationBuildTaskNames.AppSourceExtension, StringComparison.Ordinal)
            && !descriptor.EndsWith(BeamApplicationBuildTaskNames.AppExtension, StringComparison.Ordinal))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        var expected = new Dictionary<string, BeamApplicationTaskFile>(StringComparer.OrdinalIgnoreCase);
        var inputs = new Dictionary<string, string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        void Add(string source, string target, string kind)
        {
            target = BeamApplicationTaskPaths.Portable(target);
            var file = new BeamApplicationTaskFile(
                source,
                target,
                kind,
                BeamApplicationTaskPaths.Hash(source),
                new FileInfo(source).Length
            );
            if (!expected.TryAdd(target, file))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            inputs[source] = file.Sha256;
        }
        Add(
            descriptor,
            prefix + BeamApplicationBuildTaskNames.EbinPrefix + appName + BeamApplicationBuildTaskNames.AppExtension,
            BeamApplicationBuildTaskNames.DescriptorKind
        );
        var moduleRows = new List<object>();
        var relocation = new List<ITaskItem>();
        var modulePaths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (ITaskItem item in Modules)
        {
            string file = Full(item);
            string module = item.GetMetadata(BeamSourceBuildTaskNames.Module);
            if (module.Length == 0) module = Path.GetFileNameWithoutExtension(file);
            _ = BeamApplicationTaskPaths.Portable(module);
            if (module.Contains('/') || !modulePaths.Add(file) || BeamArtifactInspector.Inspect(File.ReadAllBytes(file)).Name != module)
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            string sha = BeamApplicationTaskPaths.Hash(file);
            string declared = item.GetMetadata(BeamSourceBuildTaskNames.OutputSha);
            if (declared.Length != 0 && !sha.Equals(declared, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            string defaultLink = BeamApplicationBuildTaskNames.ModulesPrefix + Path.GetFileName(file);
            ITaskItem[] existingCopies = RawNone.Where(existing => Full(existing).Equals(file, paths)).ToArray();
            if (existingCopies.Length > 1)
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            foreach (ITaskItem existing in existingCopies)
                foreach (string metadata in new[] { BeamApplicationBuildTaskNames.Link, BeamApplicationBuildTaskNames.TargetPath })
                {
                    string value = existing.GetMetadata(metadata).Replace('\\', '/');
                    if (value.Length != 0 && !value.Equals(defaultLink, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
                }
            Add(
                file,
                prefix + BeamApplicationBuildTaskNames.EbinPrefix + module + BeamBuildNames.BeamExtension,
                BeamApplicationBuildTaskNames.BeamKind
            );
            moduleRows.Add(new { Module = module, Path = file, Sha256 = sha });
            var relocated = new TaskItem(file);
            relocated.SetMetadata(BeamSourceBuildTaskNames.Module, module);
            relocated.SetMetadata(BeamApplicationBuildTaskNames.Link, prefix + BeamApplicationBuildTaskNames.EbinPrefix + module + BeamBuildNames.BeamExtension);
            relocated.SetMetadata(BeamApplicationBuildTaskNames.TargetPath, relocated.GetMetadata(BeamApplicationBuildTaskNames.Link));
            relocation.Add(relocated);
        }

        object[] Resources(ITaskItem[] values, string folder, string kind)
            => values.Select(item =>
            {
                string source = Full(item);
                string relative = BeamApplicationTaskPaths.Relative(SourceRoot, source);
                string logical = BeamApplicationTaskPaths.Portable(item.GetMetadata(BeamApplicationBuildTaskNames.LogicalPath));
                Add(source, prefix + folder + logical, kind);

                return (object)new { Source = relative, LogicalPath = logical };
            }).ToArray();
        object[] priv = Resources(Priv, BeamApplicationBuildTaskNames.PrivPrefix, BeamApplicationBuildTaskNames.PrivKind);
        object[] configuration = Resources(Configuration, BeamApplicationBuildTaskNames.ConfigurationPrefix, BeamApplicationBuildTaskNames.ConfigurationKind);
        foreach (ITaskItem content in RawContent)
            if (inputs.ContainsKey(Full(content)) && Copies(content))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        foreach (ITaskItem ordinary in RawNone)
            if (inputs.ContainsKey(Full(ordinary)) && !modulePaths.Contains(Full(ordinary))
                && (ordinary.GetMetadata(BeamApplicationBuildTaskNames.Link).Length != 0
                    || ordinary.GetMetadata(BeamApplicationBuildTaskNames.TargetPath).Length != 0))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        string manifestTarget = prefix + BeamApplicationBuildTaskNames.Manifest;
        if (expected.ContainsKey(manifestTarget))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ITaskItem ordinary in ComputedCopyItems)
        {
            if (inputs.ContainsKey(Full(ordinary))) continue;
            string output = BeamApplicationTaskPaths.Portable(ordinary.GetMetadata(BeamApplicationBuildTaskNames.TargetPath).Replace('\\', '/'));
            if (!reserved.Add(output) || expected.ContainsKey(output) || output.Equals(manifestTarget, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        }
        var request = new
        {
            InstalledRoot,
            ProvenanceKind,
            AdmissionReceipt,
            AdmissionReceiptSha256,
            CompilerSha256,
            RuntimeSha256,
            SourceRoot,
            OutputDirectory,
            ApplicationName = appName,
            Version = version,
            Descriptor = BeamApplicationTaskPaths.Relative(SourceRoot, descriptor),
            Modules = moduleRows.ToArray(),
            Priv = priv,
            Configuration = configuration,
            ReservedOutputPaths = reserved.OrderBy(value => value, StringComparer.Ordinal).ToArray()
        };
        Directory.CreateDirectory(OutputDirectory);
        string requestFile = BeamBuildStorage.WriteRequest(
            OutputDirectory,
            BeamApplicationBuildTaskNames.RequestPrefix,
            BeamSourceBuildTaskNames.RequestExtension,
            request
        );
        string requestSha = BeamApplicationTaskPaths.Hash(requestFile);
        var start = new ProcessStartInfo(DotNetHost)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = OutputDirectory
        };
        start.ArgumentList.Add(ToolAssemblyPath);
        start.ArgumentList.Add(BeamApplicationBuildTaskNames.CliMode);
        start.ArgumentList.Add(requestFile);
        using var process = Process.Start(start) ?? throw new IOException(BeamApplicationBuildTaskNames.Failed);
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
                if (!process.HasExited) process.Kill(entireProcessTree: true);
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
        if (failures.Count != 0) throw new AggregateException(BeamApplicationBuildTaskNames.Failed, failures);
        stopping.Token.ThrowIfCancellationRequested();
        if (process.ExitCode != 0) throw new InvalidDataException(BeamApplicationBuildTaskNames.Failed);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(OutputDirectory, BeamApplicationBuildTaskNames.State)));
        JsonElement state = document.RootElement;

        string Text(JsonElement owner, string name) => owner.GetProperty(name).GetString()
            ?? throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);

        void Equal(
            JsonElement owner,
            string name,
            string value,
            bool hash = true
        )
        {
            if (!Text(owner, name).Equals(value, hash ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        }
        Equal(
            state,
            BeamApplicationBuildTaskNames.ApplicationName,
            appName,
            hash: false
        );
        Equal(
            state,
            BeamApplicationBuildTaskNames.Version,
            version,
            hash: false
        );
        Equal(state, BeamApplicationBuildTaskNames.RequestSha256, requestSha);
        string configSha = Text(state, BeamApplicationBuildTaskNames.ConfigurationSha256);
        if (configSha.Length != BeamApplicationBuildTaskNames.Sha256Characters || !configSha.All(Uri.IsHexDigit))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        JsonElement evidence = state.GetProperty(BeamApplicationBuildTaskNames.NativeEvidence);
        Equal(evidence, BeamApplicationBuildTaskNames.AdmissionReceiptSha256, AdmissionReceiptSha256);
        Equal(evidence, BeamApplicationBuildTaskNames.CompilerSha256, CompilerSha256);
        Equal(evidence, BeamApplicationBuildTaskNames.RuntimeSha256, RuntimeSha256);
        Equal(evidence, BeamApplicationBuildTaskNames.ToolAssemblySha256, toolSha);
        Equal(
            evidence,
            BeamApplicationBuildTaskNames.ParsedEtfSha256,
            BeamApplicationTaskPaths.Hash(BeamApplicationTaskPaths.Absolute(Text(evidence, BeamApplicationBuildTaskNames.ParsedEtfPath)))
        );
        foreach (string name in new[] { BeamApplicationBuildTaskNames.DriverSourceSha256, BeamApplicationBuildTaskNames.DriverBeamSha256 })
            if (Text(evidence, name).Length != BeamApplicationBuildTaskNames.Sha256Characters || !Text(evidence, name).All(Uri.IsHexDigit))
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        var files = new List<ITaskItem>();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement row in state.GetProperty(BeamApplicationBuildTaskNames.Files).EnumerateArray())
        {
            string path = BeamApplicationTaskPaths.Absolute(Text(row, BeamApplicationBuildTaskNames.Path));
            _ = BeamApplicationTaskPaths.Relative(OutputDirectory, path);
            BeamApplicationTaskPaths.RejectReparse(OutputDirectory, path);
            string target = BeamApplicationTaskPaths.Portable(Text(row, BeamApplicationBuildTaskNames.TargetPath));
            string kind = Text(row, BeamApplicationBuildTaskNames.Kind);
            string sha = Text(row, BeamApplicationBuildTaskNames.Sha256);
            long bytes = row.GetProperty(BeamApplicationBuildTaskNames.Bytes).GetInt64();
            if (!found.Add(target) || reserved.Contains(target) || BeamApplicationTaskPaths.Hash(path) != sha
                || new FileInfo(path).Length != bytes)
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            if (target == manifestTarget)
            {
                if (kind != BeamApplicationBuildTaskNames.ManifestKind)
                    throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            }
            else if (!expected.TryGetValue(target, out BeamApplicationTaskFile? expectedFile)
                || expectedFile.TargetPath != target || expectedFile.Kind != kind || expectedFile.Sha256 != sha || expectedFile.Bytes != bytes)
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            var copy = new TaskItem(path);
            copy.SetMetadata(BeamApplicationBuildTaskNames.Link, target);
            copy.SetMetadata(BeamApplicationBuildTaskNames.TargetPath, target);
            copy.SetMetadata(BeamApplicationBuildTaskNames.CopyToOutput, BeamApplicationBuildTaskNames.PreserveNewest);
            copy.SetMetadata(BeamApplicationBuildTaskNames.CopyToPublish, BeamApplicationBuildTaskNames.PreserveNewest);
            files.Add(copy);
        }
        if (found.Count != expected.Count + 1 || !found.Contains(manifestTarget)
            || inputs.Any(input => BeamApplicationTaskPaths.Hash(input.Key) != input.Value)
            || BeamApplicationTaskPaths.Hash(AdmissionReceipt) != AdmissionReceiptSha256.ToUpperInvariant()
            || BeamApplicationTaskPaths.Hash(ToolAssemblyPath) != toolSha)
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        stopping.Token.ThrowIfCancellationRequested();
        AppFiles = files.ToArray();
        OriginalInputs = inputs.Keys.Select(path => (ITaskItem)new TaskItem(path)).ToArray();
        ModulesToRelocate = relocation.ToArray();
    }

    private static bool Copies(ITaskItem item)
        => new[] { BeamApplicationBuildTaskNames.CopyToOutput, BeamApplicationBuildTaskNames.CopyToPublish }
            .Any(name => item.GetMetadata(name).Length != 0 && !item.GetMetadata(name).Equals(BeamApplicationBuildTaskNames.Never, StringComparison.OrdinalIgnoreCase));

    private static string Full(ITaskItem item)
        => Path.GetFullPath(item.GetMetadata(BeamSourceBuildTaskNames.FullPath));

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

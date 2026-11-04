using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is not (FileCompatibilityArguments.RequiredCount or FileCompatibilityArguments.WithCorpusCount or FileCompatibilityArguments.WithRowCount) || args[FileCompatibilityArguments.BackendIndex] is not (FileCompatibilityReference.Managed or FileCompatibilityReference.Beam))
{
    Console.Error.WriteLine(FileCompatibilityDiagnostics.Usage);

    return FileCompatibilityExitCodes.Usage;
}
string repository = Path.GetFullPath(args[FileCompatibilityArguments.RepositoryIndex]);
string directory = Path.GetFullPath(args[FileCompatibilityArguments.DirectoryIndex]);
FileCompatibilityAdmission.Verify(repository, directory);
string corpus = args.Length >= FileCompatibilityArguments.WithCorpusCount ? args[FileCompatibilityArguments.CorpusIndex] : FileCompatibilityFiles.OriginalCorpus;
int? rowIndex = args.Length == FileCompatibilityArguments.WithRowCount ? int.Parse(args[FileCompatibilityArguments.RowIndex], System.Globalization.CultureInfo.InvariantCulture) : null;
if (corpus is not (FileCompatibilityFiles.OriginalCorpus or FileCompatibilityFiles.RuntimeGatesCorpus))
{
    throw new ArgumentException(FileCompatibilityDiagnostics.Corpus);
}
bool managed = args[FileCompatibilityArguments.BackendIndex] == FileCompatibilityReference.Managed;
string[] dependencies = FileCompatibilityModules.FileImports;
byte[] Original(string category, string name) => File.ReadAllBytes(FileCompatibilityFiles.OriginalImage(repository, category, name));
await using ProcessRuntime runtime = managed
    ? await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(FileCompatibilityModules.File)]))
    : new ProcessRuntime();
var images = new List<PreparedBeamImage>();
try
{
    void Prepare(byte[] bytes, IEnumerable<string> imports)
    {
        IEnumerable<string> foundation = Directory.EnumerateFiles(
            Path.Combine(repository, FileCompatibilityFiles.FoundationDirectory),
            FileCompatibilityFiles.BeamSearchPattern,
            SearchOption.AllDirectories
        )
            .Select(path => Path.GetFileNameWithoutExtension(path));
        images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports.Concat(foundation).Distinct(StringComparer.Ordinal))));
    }
    if (!managed)
    {
        Prepare(
            Original(FileCompatibilityFiles.KernelCategory, FileCompatibilityModules.File),
            dependencies.Append(FileCompatibilityModules.File)
        );
    }
    foreach (string name in FileCompatibilityModules.StdlibImages)
    {
        Prepare(
            Original(FileCompatibilityFiles.StdlibCategory, name),
            dependencies.Append(name).Concat(FileCompatibilityModules.StdlibImports)
        );
    }
    foreach (string name in FileCompatibilityModules.KernelImages)
    {
        Prepare(Original(FileCompatibilityFiles.KernelCategory, name), dependencies);
    }
    Prepare(Original(FileCompatibilityFiles.ErtsCategory, FileCompatibilityModules.PrimFile), dependencies);
    Prepare(
        File.ReadAllBytes(Path.Combine(directory, FileCompatibilityFiles.DeviceBeam)),
        [FileCompatibilityModules.Device, FileCompatibilityModules.Erlang, FileCompatibilityModules.Binary]
    );
    Prepare(
        File.ReadAllBytes(Path.Combine(directory, FileCompatibilityFiles.ObserverBeam)),
        dependencies.Concat([FileCompatibilityModules.File, FileCompatibilityModules.Device, FileCompatibilityModules.Observer])
    );
    await runtime.InitializeModulesAsync(registry =>
    {
        RuntimeBootstrapFileSystem.Register(runtime, registry);
        foreach (PreparedBeamImage image in images.Where(value => !value.HasOnLoad))
        {
            if (registry.PublishPreparedBeam(image).Status != BeamPublicationStatus.Published)
            {
                throw new InvalidOperationException(FileCompatibilityDiagnostics.Publication);
            }
        }
    });
    foreach (PreparedBeamImage image in images.Where(value => value.HasOnLoad))
    {
        BeamOnLoadResult loaded = await runtime.Modules.LoadPreparedOnLoadAsync(runtime, image);
        if (!loaded.Published)
        {
            throw new InvalidOperationException(FileCompatibilityDiagnostics.OnLoad + loaded);
        }
    }
    var serverReady = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle starter = runtime.Spawn(async context =>
    {
        try
        {
            _ = await runtime.Modules.Call(context, FileCompatibilityModules.PrimFile, FileCompatibilityModules.OnLoadFunction);
            serverReady.TrySetResult(await runtime.Modules.Call(
                context,
                FileCompatibilityModules.GenServer,
                FileCompatibilityModules.StartServerFunction,
                Term.Tuple(Term.A(FileCompatibilityModules.LocalRegistration), Term.A(FileCompatibilityModules.FileServerRegistration)),
                Term.A(FileCompatibilityModules.FileServer),
                Nil.Value,
                Nil.Value
            ));
        }
        catch (Exception error)
        {
            serverReady.TrySetException(error);
        }

        return Nil.Value;
    });
    Console.WriteLine(FileCompatibilityDiagnostics.Server + await serverReady.Task.WaitAsync(FileCompatibilityBudgets.ServerReady));
    await starter.Completion.WaitAsync(FileCompatibilityBudgets.ServerJoin);
    var exports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, FileCompatibilityFiles.Exports))))
        .Cast<TupleTerm>().Select(row => (((Atom)row.Items[FileCompatibilityRows.ExportNameIndex]).Name, (int)((Integer)row.Items[FileCompatibilityRows.ExportArityIndex]).Value)).ToHashSet();
    if (!exports.SetEquals(FileModule.Exports))
    {
        throw new InvalidOperationException(FileCompatibilityDiagnostics.Api);
    }
    foreach (var export in FileModule.Exports)
    {
        if (managed ? runtime.Modules.SourceOwner(FileCompatibilityModules.File, export.Name, export.Arity) is not null
            : runtime.Modules.SourceOwner(FileCompatibilityModules.File, export.Name, export.Arity) is not PreparedBeamImage)
        {
            throw new InvalidOperationException(FileCompatibilityDiagnostics.Owner);
        }
    }
    Term[] rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, corpus)))).ToArray();
    if (rowIndex is not null)
    {
        rows = [rows[rowIndex.Value]];
    }
    var differences = new List<object>();
    int count = 0;
    foreach (TupleTerm row in rows)
    {
        File.SetLastAccessTimeUtc(Path.Combine(directory, FileCompatibilityFiles.FixturePayload), FileCompatibilityFixture.AccessTimeUtc);
        var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                var sent = (TupleTerm)await runtime.Modules.Call(
                    context,
                    FileCompatibilityModules.Observer,
                    FileCompatibilityModules.ObserverWorker,
                    row.Items[FileCompatibilityRows.FunctionIndex],
                    row.Items[FileCompatibilityRows.ArgumentsIndex],
                    context.Self
                );
                completion.TrySetResult(sent.Items[FileCompatibilityRows.WorkerOutcomeIndex]);
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }

            return Nil.Value;
        });
        Term actual = await completion.Task.WaitAsync(FileCompatibilityBudgets.ProbeReady);
        await worker.Completion.WaitAsync(FileCompatibilityBudgets.ProbeJoin);
        if (!actual.Equals(row.Items[FileCompatibilityRows.ExpectedIndex]))
        {
            differences.Add(new { Function = row.Items[FileCompatibilityRows.FunctionIndex].ToString(), Arguments = row.Items[FileCompatibilityRows.ArgumentsIndex].ToString(), Expected = row.Items[FileCompatibilityRows.ExpectedIndex].ToString(), Actual = actual.ToString() });
        }
        count++;
    }
    var report = new
    {
        Backend = args[FileCompatibilityArguments.BackendIndex],
        RequestedRow = rowIndex,
        Corpus = corpus,
        Baseline = FileCompatibilityReference.Baseline,
        Exports = exports.Count,
        Compared = count,
        Skipped = rows.Length - count,
        Differences = differences,
        AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(FileModule).Assembly.Location))),
        RunnerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(FileCompatibilityAdmission).Assembly.Location))),
        RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
        CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, corpus))))
    };
    string reportName = FileCompatibilityFiles.Report(args[FileCompatibilityArguments.BackendIndex], corpus, rowIndex);
    await File.WriteAllTextAsync(
        Path.Combine(directory, reportName),
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
    );
    Console.WriteLine(FileCompatibilityDiagnostics.Results(
        args[FileCompatibilityArguments.BackendIndex],
        count,
        differences.Count,
        rows.Length - count
    ));

    return differences.Count == 0 ? FileCompatibilityExitCodes.Success : FileCompatibilityExitCodes.Differences;
}
finally
{
    foreach (PreparedBeamImage image in images)
    {
        image.Dispose();
    }
}

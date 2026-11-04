using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length < GenEventValidationArguments.RequiredCount || args[GenEventValidationArguments.BackendIndex] is not (nameof(RuntimeModuleBackend.Managed) or nameof(RuntimeModuleBackend.Beam)))
{
    Console.Error.WriteLine(GenEventValidationDiagnostics.Usage);

    return GenEventValidationArguments.UsageExitCode;
}
string repository = Path.GetFullPath(args[GenEventValidationArguments.RepositoryIndex]);
string directory = Path.GetFullPath(args[GenEventValidationArguments.CorpusIndex]);
bool managed = args[GenEventValidationArguments.BackendIndex] == nameof(RuntimeModuleBackend.Managed);
int? backtraceDepth = args.Length == GenEventValidationArguments.OptionalCount && args[GenEventValidationArguments.CaseIndex].StartsWith(GenEventValidationArguments.DepthPrefix, StringComparison.Ordinal)
    ? int.Parse(
        args[GenEventValidationArguments.CaseIndex][GenEventValidationArguments.DepthPrefix.Length..],
        System.Globalization.CultureInfo.InvariantCulture
    ) : null;
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var assemblies = new[] { typeof(GenEventModule).Assembly.Location, typeof(ProcessRuntime).Assembly.Location,
    typeof(ExternalTermFormat).Assembly.Location, typeof(Erlang.Otp.OtpModules).Assembly.Location }
    .ToDictionary(path => Path.GetFileName(path)!, Hash);
var sources = Directory.EnumerateFiles(Path.Combine(repository, GenEventValidationFiles.SourceDirectory), GenEventValidationFiles.CSharpPattern)
    .Concat(Directory.EnumerateFiles(Path.Combine(repository, GenEventValidationFiles.ObserverDirectory), GenEventValidationFiles.ErlangPattern))
    .ToDictionary(path => Path.GetRelativePath(repository, path).Replace('\\', '/'), Hash);
var inputImages = new Dictionary<string, string>();
string foundation = Path.Combine(repository, GenEventValidationFiles.FoundationDirectory);
var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(GenEventModule.Name)] : []),
    RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.EtsStorageCore,
    CancellationToken.None
);
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeBootstrapFileSystem.Register(runtime, registry);
    Erlang.Otp.OtpModules.Register(registry);
});

var images = new List<PreparedBeamImage>();
string[] modules = [GenEventValidationModules.Gen, GenEventValidationModules.Sys, GenEventValidationModules.ProcLib, GenEventValidationModules.IoLib, GenEventValidationModules.IoLibFormat, GenEventValidationModules.IoLibPretty, GenEventValidationModules.Unicode, GenEventValidationModules.UnicodeUtil, GenEventValidationModules.Code, GenEventValidationModules.Epp, GenEventValidationModules.ErlScan, GenEventValidationModules.ErlFeatures, GenEventValidationModules.String, GenEventValidationModules.Init, GenEventValidationModules.ErlPrimLoader, GenEventValidationModules.PrimFile, GenEventValidationModules.Os, GenEventValidationModules.Filename, GenEventValidationModules.GbSets, GenEventValidationModules.GbTrees, GenEventValidationModules.File, GenEventValidationModules.CodeServer, GenEventValidationModules.Io];
string[] dependencies = runtime.Modules.Exports.Select(export => export.Module)
    .Concat(Directory.EnumerateFiles(
        Path.Combine(repository, GenEventValidationFiles.OriginalDirectory),
        GenEventValidationFiles.BeamPattern,
        SearchOption.AllDirectories
    ).Select(Path.GetFileNameWithoutExtension)!)
    .Concat([GenEventValidationModules.GenEventPortCases, GenEventValidationModules.GenEventNoOptional, GenEventValidationModules.Logger, GenEventValidationModules.LoggerServer, GenEventValidationModules.Global, GenEventValidationModules.Code, GenEventValidationModules.Application, GenEventValidationModules.NetKernel, GenEventValidationModules.LoggerFormatter, GenEventValidationModules.LoggerFilters]).Distinct().ToArray()!;
if (!managed)
{
    images.Add(runtime.Modules.PrepareBeam(
        File.ReadAllBytes(Path.Combine(foundation, GenEventValidationFiles.OriginalImage)),
        new BeamDependencySelection(dependencies)
    ));
}
foreach (string module in modules)
{

    string path = Directory.EnumerateFiles(
        Path.Combine(repository, GenEventValidationFiles.OriginalDirectory),
        module + GenEventValidationFiles.BeamExtension,
        SearchOption.AllDirectories
    ).First();
    inputImages[Path.GetRelativePath(repository, path).Replace('\\', '/')] = Hash(path);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies)));
}
foreach (string fixture in new[] { GenEventValidationModules.GenEventPortCases, GenEventValidationModules.GenEventNoOptional, GenEventValidationModules.GenEventSUITE, GenEventValidationModules.DummyH, GenEventValidationModules.Dummy1H, GenEventValidationModules.DummyVia })
{
    images.Add(runtime.Modules.PrepareBeam(
        File.ReadAllBytes(Path.Combine(directory, fixture + GenEventValidationFiles.BeamExtension)),
        new BeamDependencySelection(dependencies.Concat([GenEventValidationModules.GenEventSUITE, GenEventValidationModules.DummyH, GenEventValidationModules.Dummy1H, GenEventValidationModules.DummyVia, GenEventValidationModules.Ct, GenEventValidationModules.TestServer, GenEventValidationModules.ErrorLoggerForwarder, GenEventValidationModules.OcEvent]))
    ));
}
await runtime.InitializeModulesAsync(registry =>
{
    foreach (PreparedBeamImage image in images)
    {
        BeamPublicationResult result = registry.PublishPreparedBeam(image);
        if (result.Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(GenEventValidationDiagnostics.Publication(image.Name, result.Status));
        }
    }
});
await GenEventBoot.Start(runtime, directory);
var loggerSetup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle setupProcess = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            GenEventValidationModules.Logger,
            GenEventValidationFunctions.SetPrimaryConfig,
            Term.A(GenEventValidationAtoms.Level),
            Term.A(GenEventValidationAtoms.Emergency)
        );
        if (backtraceDepth is int depth)
        {
            await runtime.Modules.Call(
                context,
                GenEventValidationModules.Erlang,
                GenEventValidationFunctions.SystemFlag,
                Term.A(GenEventValidationAtoms.BacktraceDepth),
                Term.I(depth)
            );
        }
        loggerSetup.SetResult();
    }
    catch (Exception error)
    {
        loggerSetup.SetException(error);
    }

    return Nil.Value;
});
await loggerSetup.Task.WaitAsync(GenEventValidationBudgets.Initialization);
await setupProcess.Completion.WaitAsync(GenEventValidationBudgets.StartupWorkerExit);
RuntimeLibraryModuleSnapshot? selected = runtime.LibrarySelection.Modules.SingleOrDefault(value => value.Module == GenEventModule.Name);
foreach (var export in GenEventModule.Exports)
{
    object? owner = runtime.Modules.SourceOwner(GenEventModule.Name, export.Name, export.Arity);
    if (managed ? owner is not null : owner is not PreparedBeamImage image || image.OriginalBeamSha256 != Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(foundation, GenEventValidationFiles.OriginalImage)))))
    {
        throw new InvalidOperationException(GenEventValidationDiagnostics.BackendOwnerMismatch);
    }
}
Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, GenEventValidationFiles.Exports)));
if (!GenEventModule.Exports.ToHashSet().SetEquals(Cons.Items(declared).Cast<TupleTerm>().Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value))))
{
    throw new InvalidOperationException(GenEventValidationDiagnostics.ExportMismatch);
}
var failures = new List<object>();
int passed = 0;
int attempted = 0;
Term[] rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, GenEventValidationFiles.Corpus)))).ToArray();
foreach (TupleTerm row in rows)
{
    string name = row.Items[0] is Atom atom ? atom.Name : row.Items[0].ToString()!;
    if (args.Length == GenEventValidationArguments.OptionalCount && backtraceDepth is null && args[GenEventValidationArguments.CaseIndex] != name)
    {
        continue;
    }
    attempted++;
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle process = runtime.Spawn(async context =>
    {
        try
        {
            var sent = (TupleTerm)await runtime.Modules.Call(
                context,
                GenEventValidationModules.GenEventPortCases,
                GenEventValidationFunctions.Worker,
                row.Items[0],
                context.Self
            );
            completion.TrySetResult(sent.Items[1]);
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });
    try
    {
        Term actual = await completion.Task.WaitAsync(GenEventValidationBudgets.Observation);
        await process.Completion.WaitAsync(GenEventValidationBudgets.ObservationWorkerExit);
        if (!actual.Equals(row.Items[1]))
        {
            failures.Add(new { Case = name, Expected = row.Items[1].ToString(), Actual = actual.ToString() });
            Console.WriteLine(GenEventValidationDiagnostics.Difference(name, actual));
        }
        else
        {
            passed++;
            Console.WriteLine(GenEventValidationDiagnostics.Passed(name));
        }
    }
    catch (Exception error)
    {
        failures.Add(new { Case = name, Error = error.ToString() });
        Console.WriteLine(GenEventValidationDiagnostics.Failed(name, error));
        break;
    }
}
try
{
    await runtime.DisposeAsync().AsTask().WaitAsync(GenEventValidationBudgets.RuntimeDisposal);
}
catch (Exception error)
{
    failures.Add(new { Case = GenEventValidationDiagnostics.DisposalCase, Error = error.ToString() });
}
foreach (var input in inputImages.Concat(sources))
{
    if (Hash(Path.Combine(repository, input.Key)) != input.Value)
    {
        failures.Add(new { Case = GenEventValidationDiagnostics.ChangedInputCase, File = input.Key });
    }
}
File.WriteAllText(
    Path.Combine(
        directory,
        args[GenEventValidationArguments.BackendIndex] + (args.Length == GenEventValidationArguments.OptionalCount ? GenEventValidationFiles.ReportOptionSeparator + args[GenEventValidationArguments.CaseIndex] : string.Empty) + GenEventValidationFiles.ReportSuffix
    ),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[GenEventValidationArguments.BackendIndex],
            Baseline = GenEventValidationContract.Baseline,
            Commit = GenEventValidationContract.Commit,
            Cases = rows.Length,
            Attempted = attempted,
            CompleteCorpus = (args.Length == GenEventValidationArguments.RequiredCount || backtraceDepth is not null) && attempted == rows.Length,
            BacktraceDepth = backtraceDepth,
            Assemblies = assemblies,
            Sources = sources,
            Inputs = inputImages,
            ObserverSha256 = Hash(Path.Combine(directory, GenEventValidationFiles.ObserverImage)),
            Exports = GenEventModule.Exports.Count,
            Passed = passed,
            Failures = failures,
            InputSha256 = managed ? null : Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(foundation, GenEventValidationFiles.OriginalImage)))),
            ManagedAssemblies = selected?.ManagedAssemblies,
            CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, GenEventValidationFiles.Corpus))))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);

return failures.Count == 0 ? GenEventValidationArguments.SuccessExitCode : GenEventValidationArguments.FailureExitCode;

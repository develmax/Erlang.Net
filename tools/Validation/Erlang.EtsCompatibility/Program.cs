using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is not (3 or 4) || args[2] is not (EtsValidationOptions.Managed or EtsValidationOptions.Beam)
    || args.Length == 4 && args[3] is not (EtsValidationOptions.Files or EtsValidationOptions.Browse))
{
    Console.Error.WriteLine(EtsValidationMessages.Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == EtsValidationOptions.Managed;
bool files = args.Length == 4 && args[3] == EtsValidationOptions.Files;
bool browse = args.Length == 4 && args[3] == EtsValidationOptions.Browse;
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(EtsModule.Name)] : []),
    RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string stdlib = Path.Combine(repository, EtsValidationFiles.Stdlib);
string kernel = Path.Combine(repository, EtsValidationFiles.Kernel);
string erts = Path.Combine(repository, EtsValidationFiles.Erts);
string[] dependencies = Directory.GetFiles(Path.Combine(repository, EtsValidationFiles.OriginalRoot), EtsValidationFiles.BeamPattern, SearchOption.AllDirectories)
    .Select(Path.GetFileNameWithoutExtension).Cast<string>().Append(EtsValidationModules.Erlang).Append(EtsValidationModules.EtsPortCases).Append(EtsValidationModules.EtsPortDevice).Append(EtsValidationModules.EtsPortSupport).Distinct().ToArray();
var images = new List<PreparedBeamImage>();
RuntimeRegexEngine411? regexEngine = null;
await runtime.InitializeModulesAsync(registry =>
{
    if (!managed)
    {
        RuntimeBootstrapEtsLibrary.Register(runtime, registry);
    }
    if (files)
    {
        RuntimeBootstrapFileSystem.Register(runtime, registry);
    }
});
if (browse)
{
    regexEngine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules,
        Path.Combine(repository, EtsValidationFiles.Admission));
}
string[] fileHelpers = [EtsValidationModules.File, EtsValidationModules.RawFileIo, EtsValidationModules.FileServer, EtsValidationModules.FileIoServer, EtsValidationModules.Os, EtsValidationModules.RamFile, EtsValidationModules.RawFileIoList,
    EtsValidationModules.RawFileIoDelayed, EtsValidationModules.RawFileIoCompressed, EtsValidationModules.RawFileIoDeflate, EtsValidationModules.RawFileIoInflate, EtsValidationModules.PrimFile, EtsValidationModules.PrimBuffer, EtsValidationModules.EtsPortSupport,
    EtsValidationModules.GenServer, EtsValidationModules.Gen, EtsValidationModules.ProcLib, EtsValidationModules.Sys, EtsValidationModules.Supervisor, EtsValidationModules.DiskLog, EtsValidationModules.DiskLogServer, EtsValidationModules.DiskLogSup, EtsValidationModules.DiskLog1,
    EtsValidationModules.Logger, EtsValidationModules.LoggerConfig, EtsValidationModules.LoggerBackend, EtsValidationModules.LoggerFilters, EtsValidationModules.LoggerServer, EtsValidationModules.LoggerProxy, EtsValidationModules.IoLib, EtsValidationModules.IoLibFormat,
    EtsValidationModules.Io, EtsValidationModules.Filename, EtsValidationModules.Unicode, EtsValidationModules.UnicodeUtil, EtsValidationModules.Binary, EtsValidationModules.Proplists, EtsValidationModules.ErlPosixMsg,
    EtsValidationModules.DetsServer, EtsValidationModules.DetsSup, EtsValidationModules.DetsUtils, EtsValidationModules.DetsV9, EtsValidationModules.Init, EtsValidationModules.Timer, EtsValidationModules.FileSorter];
foreach (string module in new[] { EtsValidationModules.Lists, EtsValidationModules.Qlc, EtsValidationModules.GbTrees, EtsValidationModules.GbSets, EtsValidationModules.Sets, EtsValidationModules.Maps, EtsValidationModules.Orddict, EtsValidationModules.Ordsets, EtsValidationModules.Sofs, EtsValidationModules.ErlEval, EtsValidationModules.MsTransform, EtsValidationModules.Dets,
    EtsValidationModules.ErlAnno, EtsValidationModules.ErlParse, EtsValidationModules.ErlLint, EtsValidationModules.ErlInternal, EtsValidationModules.ErlScan, EtsValidationModules.EvalBits, EtsValidationModules.ErlFeatures, EtsValidationModules.Os }
    .Concat(files ? fileHelpers : []).Distinct()
    .Concat(browse ? new[] { EtsValidationModules.Io, EtsValidationModules.IoLib, EtsValidationModules.IoLibFormat, EtsValidationModules.IoLibPretty, EtsValidationModules.Unicode, EtsValidationModules.UnicodeUtil, EtsValidationModules.String, EtsValidationModules.Binary, EtsValidationModules.Re, EtsValidationModules.NetKernel, EtsValidationModules.Epp, EtsValidationModules.EtsPortDevice } : [])
    .Concat(managed ? [] : new[] { EtsValidationModules.Ets }).Append(EtsValidationModules.EtsPortCases))
{
    string file = module is EtsValidationModules.EtsPortCases or EtsValidationModules.EtsPortDevice or EtsValidationModules.EtsPortSupport ? Path.Combine(directory, module + EtsValidationFiles.BeamExtension)
        : new[] { stdlib, kernel, erts }.Select(path => Path.Combine(path, module + EtsValidationFiles.BeamExtension)).First(File.Exists);
    Console.WriteLine(EtsValidationMessages.PreparationPrefix + module);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(file), new BeamDependencySelection(dependencies)));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.Where(value => !value.HasOnLoad).ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(EtsValidationMessages.PublicationFailed);
    }
});
foreach (PreparedBeamImage image in images.Where(value => value.HasOnLoad))
{
    BeamOnLoadResult loaded = await runtime.Modules.LoadPreparedOnLoadAsync(runtime, image);
    if (!loaded.Published)
    {
        throw new InvalidOperationException(EtsValidationMessages.OnLoadFailed + loaded);
    }
}
// Publication consumes the image; the caller still owns its preparation lease.
// Release that lease before VM teardown, including runtime-intrinsic literals.
foreach (PreparedBeamImage image in images)
{
    await image.DisposeAsync();
}
await Run(async context =>
{
    foreach (TupleTerm field in Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, EtsValidationFiles.FeaturesEtf)))))
    {
        await runtime.Modules.Call(
            context,
            EtsValidationModules.PersistentTerm,
            EtsValidationFunctions.Put,
            Term.Tuple(Term.A(EtsValidationModules.ErlFeatures), field.Items[0]),
            field.Items[1]
        );
    }
    await runtime.Modules.Call(
        context,
        EtsValidationModules.PersistentTerm,
        EtsValidationFunctions.Put,
        Term.Tuple(Term.A(EtsValidationModules.ErlFeatures), Term.A(EtsValidationAtoms.InitDone)),
        Term.A(EtsValidationAtoms.True)
    );

    return Term.A(EtsValidationAtoms.Ok);
});
if (files)
{
    Console.WriteLine(EtsValidationMessages.FileServerPrefix + await Run(async context =>
    {
        await runtime.Modules.Call(
            context,
            EtsValidationModules.PersistentTerm,
            EtsValidationFunctions.Put,
            Term.Tuple(Term.A(EtsValidationModules.LoggerConfig), Term.A(EtsValidationAtoms.PrimaryConfig)),
            Term.I(-1)
        );
        await runtime.Modules.Call(context, EtsValidationModules.PrimFile, EtsValidationFunctions.OnLoad);
        await runtime.Modules.Call(context, EtsValidationModules.PrimBuffer, EtsValidationFunctions.OnLoad);

        return await runtime.Modules.Call(
            context,
            EtsValidationModules.GenServer,
            EtsValidationFunctions.Start,
            Term.Tuple(Term.A(EtsValidationAtoms.Local), Term.A(EtsValidationAtoms.FileServer2)),
            Term.A(EtsValidationModules.FileServer),
            Nil.Value,
            Nil.Value
        );
    }));
    var supervisorReady = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    _ = runtime.Spawn(async context =>
    {
        try
        {
            supervisorReady.TrySetResult(await runtime.Modules.Call(
                context,
                EtsValidationModules.Supervisor,
                EtsValidationFunctions.StartLink,
                Term.Tuple(Term.A(EtsValidationAtoms.Local), Term.A(EtsValidationAtoms.KernelSafeSup)),
                Term.A(EtsValidationModules.EtsPortSupport),
                Nil.Value
            ));
            await context.ReceiveAsync(value => value is Atom { Name: EtsValidationFunctions.Stop } ? value : null);
        }
        catch (Exception error)
        {
            supervisorReady.TrySetException(error);
        }

        return Nil.Value;
    });
    Console.WriteLine(EtsValidationMessages.SupervisorPrefix + await supervisorReady.Task.WaitAsync(TimeSpan.FromSeconds(EtsValidationSettings.SupervisorTimeoutSeconds)));
}
var declared = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, EtsValidationFiles.ExportsEtf))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
var actual = runtime.Modules.Exports.Where(export => export.Module == EtsModule.Name).Select(export => (export.Function, export.Arity)).ToHashSet();
if (!declared.SetEquals(actual))
{
    throw new InvalidOperationException(EtsValidationMessages.ExportMismatch + string.Join(',', declared.Except(actual))
        + EtsValidationMessages.ExtraExports + string.Join(',', actual.Except(declared)));
}
if (!EtsModule.Exports.ToHashSet().SetEquals(declared.Where(export => export.Name != EtsValidationFunctions.ModuleInfo)))
{
    throw new InvalidOperationException(EtsValidationMessages.InventoryMismatch);
}
PreparedBeamImage? etsImage = managed ? null : images.Single(image => image.Name == EtsModule.Name);
var native = managed ? runtime.LibrarySelection.Modules.Single(module => module.Module == EtsModule.Name).NativePrimitives
    : etsImage!.NativePrimitiveExports;
if (!EtsModule.NativeExports.ToHashSet().SetEquals(native.Select(export => (export.Name, export.Arity))))
{
    throw new InvalidOperationException(EtsValidationMessages.NativeMismatch);
}
foreach (var export in actual)
{
    object? owner = runtime.Modules.SourceOwner(EtsModule.Name, export.Function, export.Arity);
    if (managed ? owner is not null : !ReferenceEquals(owner, etsImage))
    {
        throw new InvalidOperationException(EtsValidationMessages.OwnerMismatch);
    }
}
var failures = new List<object>();
int passed = 0;
Term[] rows = files || browse ? [] : Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, EtsValidationFiles.OriginalEtf))).Term).ToArray();
foreach (TupleTerm row in rows)
{
    var sent = (TupleTerm)await Run(context => runtime.Modules.Call(
        context,
        EtsValidationModules.EtsPortCases,
        EtsValidationFunctions.Worker,
        row.Items[0],
        row.Items[1],
        context.Self
    ));
    Term result = sent.Items[1];
    if (result.Equals(row.Items[2]))
    {
        passed++;
    }
    else
    {
        failures.Add(new { Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = row.Items[2].ToString(), Actual = result.ToString() });
    }
}
int effects = 0;
string effectFile = files ? EtsValidationFiles.FilesystemEtf : browse ? EtsValidationFiles.BrowseEtf : EtsValidationFiles.EffectsEtf;
Term[] effectRows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, effectFile)))).ToArray();
foreach (TupleTerm row in effectRows)
{
    Console.WriteLine(EtsValidationMessages.ScenarioPrefix + row.Items[0]);
    var sent = (TupleTerm)await Run(context => runtime.Modules.Call(
        context,
        EtsValidationModules.EtsPortCases,
        EtsValidationFunctions.ScenarioWorker,
        row.Items[0],
        context.Self
    ));
    Term result = sent.Items[1];
    if (result.Equals(row.Items[1]))
    {
        effects++;
    }
    else
    {
        failures.Add(new { Scenario = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = result.ToString() });
    }
}
bool regexDataBalanced = regexEngine is null || regexEngine.Report.MatchDataCreated == regexEngine.Report.MatchDataFreed;
if (!regexDataBalanced)
{
    failures.Add(new { RegexMatchDataLeak = regexEngine!.Report });
}
string? exportedDump = null;
bool? exportedDataMatched = null;
if (files)
{
    exportedDump = Path.Combine(directory, args[2] + EtsValidationFiles.DumpExtension);
    Term exported = await Run(context => runtime.Modules.Call(
        context,
        EtsValidationModules.EtsPortCases,
        EtsValidationFunctions.MakeDump,
        Term.String(exportedDump)
    ));
    exportedDataMatched = exported.Equals(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, EtsValidationFiles.DumpDataEtf))));
    if (exportedDataMatched != true)
    {
        failures.Add(new { ExportedDump = exportedDump, Actual = exported.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(
        directory,
        args[2] + (files ? EtsValidationFiles.FilesSuffix : browse ? EtsValidationFiles.BrowseSuffix : string.Empty) + EtsValidationFiles.ReportSuffix
    ),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = EtsValidationSettings.Baseline,
            Exports = actual.Count,
            PublicExports = EtsModule.Exports.Count,
            LibraryExports = EtsModule.LibraryExports.Count,
            NativeExports = native.Count,
            Cases = rows.Length,
            Passed = passed,
            Effects = effects,
            EffectCases = effectRows.Length,
            EffectFile = effectFile,
            CompleteCorpus = true,
            RegexDataBalanced = regexDataBalanced,
            ExportedDataMatched = exportedDataMatched,
            ExportedDumpSha256 = exportedDump is not null && File.Exists(exportedDump) ? Digest(exportedDump) : null,
            Failures = failures,
            ManagedAssemblySha256 = Digest(typeof(EtsModule).Assembly.Location),
            RuntimeAssemblySha256 = Digest(typeof(ProcessRuntime).Assembly.Location),
            EtsBeamSha256 = Digest(Path.Combine(stdlib, EtsValidationFiles.EtsBeam)),
            CorpusSha256 = Digest(Path.Combine(directory, EtsValidationFiles.OriginalEtf)),
            EffectsSha256 = Digest(Path.Combine(directory, effectFile)),
            FeatureStateSha256 = Digest(Path.Combine(directory, EtsValidationFiles.FeaturesEtf)),
            ObserverBeamSha256 = Digest(Path.Combine(directory, EtsValidationFiles.EtsPortCasesBeam)),
            DeviceBeamSha256 = Digest(Path.Combine(directory, EtsValidationFiles.EtsPortDeviceBeam))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(string.Format(
    System.Globalization.CultureInfo.InvariantCulture,
    EtsValidationMessages.Summary,
    args[2],
    passed,
    rows.Length,
    effects,
    failures.Count
));

return failures.Count == 0 ? 0 : 1;

static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
{
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await body(context));
        }
        catch (ErlangException error)
        {
            result.TrySetResult(Term.Tuple(
                Term.A(EtsValidationAtoms.Uncaught),
                Term.A(error.ExceptionClass),
                error.Reason,
                error.StackTraceTerm
            ));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term value = await result.Task.WaitAsync(TimeSpan.FromSeconds(EtsValidationSettings.WorkerTimeoutSeconds));
    await worker.Completion.WaitAsync(TimeSpan.FromSeconds(EtsValidationSettings.CompletionTimeoutSeconds));

    return value;
}

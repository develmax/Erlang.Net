using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is < GenStatemValidationLimits.RequiredArguments or > GenStatemValidationLimits.ArgumentsWithScenario || args[2] is not (GenStatemValidationBaseline.ManagedBackend or GenStatemValidationBaseline.BeamBackend))
{
    Console.Error.WriteLine(GenStatemValidationDiagnostics.Usage);

    return GenStatemValidationLimits.UsageExitCode;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == GenStatemValidationBaseline.ManagedBackend;
bool retentionOnly = args.Length == GenStatemValidationLimits.ArgumentsWithScenario && args[3] == GenStatemRetentionNames.Scenario;
var retention = new GenStatemRetentionChecks();
string stdlib = Path.Combine(repository, GenStatemValidationPaths.Stdlib);
string kernel = Path.Combine(repository, GenStatemValidationPaths.Kernel);
string erts = Path.Combine(repository, GenStatemValidationPaths.Erts);
GenStatemAdmission.Verify(repository, directory, stdlib);
string[] dependencies = Directory.GetFiles(stdlib, GenStatemValidationPaths.BeamPattern).Concat(Directory.GetFiles(kernel, GenStatemValidationPaths.BeamPattern))
    .Select(Path.GetFileNameWithoutExtension).Select(name => name!).Append(GenStatemValidationModules.Erlang).Append(GenStatemValidationModules.ErtsInternal).Append(GenStatemValidationModules.ErlPrimLoader).Append(GenStatemValidationModules.GenStatemPortCases).Distinct().ToArray();
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(GenStatemModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string[] helpers = [GenStatemValidationModules.Gen, GenStatemValidationModules.ProcLib, GenStatemValidationModules.Sys, GenStatemValidationModules.Lists, GenStatemValidationModules.Maps, GenStatemValidationModules.Queue, GenStatemValidationModules.Ets, GenStatemValidationModules.Logger, GenStatemValidationModules.LoggerConfig,
    GenStatemValidationModules.LoggerFilters, GenStatemValidationModules.LoggerBackend, GenStatemValidationModules.LoggerServer, GenStatemValidationModules.LoggerProxy, GenStatemValidationModules.Proplists, GenStatemValidationModules.IoLib, GenStatemValidationModules.IoLibFormat, GenStatemValidationModules.IoLibPretty, GenStatemValidationModules.Io, GenStatemValidationModules.ErrorLogger, GenStatemValidationModules.Application, GenStatemValidationModules.ApplicationController,
    GenStatemValidationModules.String, GenStatemValidationModules.Unicode, GenStatemValidationModules.UnicodeUtil, GenStatemValidationModules.Epp, GenStatemValidationModules.ErlScan, GenStatemValidationModules.ErlFeatures, GenStatemValidationModules.Code,
    GenStatemValidationModules.Init, GenStatemValidationModules.ErlPrimLoader, GenStatemValidationModules.CodeServer, GenStatemValidationModules.PrimFile, GenStatemValidationModules.Filename, GenStatemValidationModules.Os, GenStatemValidationModules.GbSets, GenStatemValidationModules.GbTrees, GenStatemValidationModules.File];
var images = new List<PreparedBeamImage>();
await runtime.InitializeModulesAsync(registry =>
{
    GenStatemValidationPrimitives.Register(runtime, registry);
    RuntimeBootstrapFileSystem.Register(runtime, registry);
    if (retentionOnly)
    {
        retention.Register(registry);
    }
});
foreach (string module in helpers.Concat(managed ? [] : new[] { GenStatemModule.Name }).Concat([GenStatemValidationModules.GenStatemPortCases, GenStatemValidationModules.GenStatemPortSecond]))
{
    Console.WriteLine(GenStatemValidationDiagnostics.PreparePrefix + module);
    string file = module is GenStatemValidationModules.GenStatemPortCases or GenStatemValidationModules.GenStatemPortSecond ? Path.Combine(directory, module + GenStatemValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(stdlib, module + GenStatemValidationPaths.BeamExtension)) ? Path.Combine(stdlib, module + GenStatemValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(kernel, module + GenStatemValidationPaths.BeamExtension)) ? Path.Combine(kernel, module + GenStatemValidationPaths.BeamExtension) : Path.Combine(erts, module + GenStatemValidationPaths.BeamExtension);
    try
    {
        byte[] bytes = File.ReadAllBytes(file);
        images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(dependencies.Concat(GenStatemBeamImports.Read(runtime.Modules, bytes)))));
    }
    catch (Exception error)
    {
        foreach (System.Collections.DictionaryEntry entry in error.Data)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(entry.Value));
        }
        throw;
    }
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(GenStatemValidationDiagnostics.PublicationFailed);
    }
});
foreach (var export in GenStatemModule.Exports)
{
    if (!runtime.Modules.Exports.Contains((GenStatemModule.Name, export.Name, export.Arity))
        || managed != (runtime.Modules.SourceOwner(GenStatemModule.Name, export.Name, export.Arity) is null))
    {
        throw new InvalidOperationException(GenStatemValidationDiagnostics.BackendOwnerMismatch);
    }
}
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenStatemValidationPaths.Corpus))).Term).ToArray();
var environmentReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle environment = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            GenStatemValidationModules.Ets,
            GenStatemValidationProtocol.New,
            Term.A(GenStatemValidationProtocol.ApplicationTable),
            Term.List(Term.A(GenStatemValidationProtocol.NamedTable), Term.A(GenStatemValidationProtocol.Public))
        );
        Term loggerTable = await runtime.Modules.Call(
            context,
            GenStatemValidationModules.LoggerConfig,
            GenStatemValidationProtocol.New,
            Term.A(GenStatemValidationProtocol.LoggerTable)
        );
        Term primary = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(GenStatemValidationProtocol.Level)] = Term.A(GenStatemValidationProtocol.None),
            [Term.A(GenStatemValidationProtocol.Filters)] = Nil.Value,
            [Term.A(GenStatemValidationProtocol.FilterDefault)] = Term.A(GenStatemValidationProtocol.Log),
            [Term.A(GenStatemValidationProtocol.Handlers)] = Nil.Value,
            [Term.A(GenStatemValidationProtocol.Metadata)] = new MapTerm([])
        });
        await runtime.Modules.Call(
            context,
            GenStatemValidationModules.LoggerConfig,
            GenStatemValidationProtocol.Create,
            loggerTable,
            Term.A(GenStatemValidationProtocol.Primary),
            primary
        );
        foreach (TupleTerm entry in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenStatemValidationPaths.FeatureBootstrap))).Term))
        {
            await runtime.Modules.Call(
                context,
                GenStatemValidationModules.PersistentTerm,
                GenStatemValidationProtocol.Put,
                entry.Items[0],
                entry.Items[1]
            );
        }
        environmentReady.TrySetResult();
        await context.ReceiveAsync(message => message);

        return Nil.Value;
    }
    catch (Exception error)
    {
        environmentReady.TrySetException(error);
        throw;
    }
});
await environmentReady.Task.WaitAsync(GenStatemValidationLimits.OperationDeadline);
await Run(context => runtime.Modules.Call(context, GenStatemValidationModules.PrimFile, GenStatemValidationProtocol.OnLoad));
using (var bootDeadline = new CancellationTokenSource(GenStatemValidationLimits.BootDeadline))
{
    RuntimeOtpBootSession boot = await runtime.StartOtpBootAsync(GenStatemCodeBoot.Create(repository, directory), bootDeadline.Token);
    if (boot.Completion.IsCompleted)
    {
        throw new InvalidOperationException(GenStatemValidationDiagnostics.BootEnded);
    }
}
if (retentionOnly)
{
    await retention.Run(runtime, directory);
    await Run(async context => await runtime.Modules.Call(
        context,
        GenStatemValidationModules.Erlang,
        GenStatemValidationProtocol.Send,
        environment.Pid,
        Term.A(GenStatemValidationProtocol.Done)
    ));
    await environment.Completion.WaitAsync(GenStatemValidationLimits.OperationDeadline);

    return GenStatemValidationLimits.SuccessExitCode;
}
Term[] effects = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenStatemValidationPaths.Effects))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
int effectPassed = 0;
int effectsRun = 0;
if (args.Length == GenStatemValidationLimits.RequiredArguments)
{
    foreach (TupleTerm row in rows)
    {
        try
        {
            Term actual = await Run(context => runtime.Modules.Call(
                context,
                GenStatemValidationModules.GenStatemPortCases,
                GenStatemValidationProtocol.Observe,
                row.Items[0],
                row.Items[1]
            ));
            if (actual.Equals(row.Items[2]))
            {
                passed++;
            }
            else
            {
                failures.Add(new { Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = row.Items[2].ToString(), Actual = actual.ToString() });
            }
        }
        catch (Exception error)
        {
            failures.Add(new { Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Failure = error.ToString() });
        }
    }
}
foreach (TupleTerm row in effects)
{
    if (args.Length == GenStatemValidationLimits.ArgumentsWithScenario && args[3] != (row.Items[0] is Atom named ? named.Name : row.Items[0].ToString()))
    {
        continue;
    }
    effectsRun++;
    Console.WriteLine(GenStatemValidationDiagnostics.ScenarioPrefix + row.Items[0]);
    try
    {
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            GenStatemValidationModules.GenStatemPortCases,
            GenStatemValidationProtocol.Scenario,
            row.Items[0]
        ));
        if (actual.Equals(row.Items[1]))
        {
            effectPassed++;
        }
        else
        {
            failures.Add(new { Scenario = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
        }
    }
    catch (Exception error)
    {
        failures.Add(new { Scenario = row.Items[0].ToString(), Failure = error.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(
        directory,
        args[2] + (args.Length == GenStatemValidationLimits.ArgumentsWithScenario ? GenStatemValidationPaths.ScenarioSeparator + args[3] : "") + GenStatemValidationPaths.ReportSuffix
    ),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = GenStatemValidationBaseline.Release,
            Exports = GenStatemModule.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            EffectCases = effects.Length,
            EffectPassed = effectPassed,
            EffectsRun = effectsRun,
            Complete = args.Length == GenStatemValidationLimits.RequiredArguments && passed == rows.Length && effectPassed == effects.Length && failures.Count == 0,
            Failures = failures,
            OriginalSha256 = Hash(Path.Combine(stdlib, GenStatemValidationPaths.OriginalBeam)),
            ManagedAssemblySha256 = Hash(typeof(GenStatemModule).Assembly.Location),
            RuntimeAssemblySha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            CorpusSha256 = Hash(Path.Combine(directory, GenStatemValidationPaths.Corpus)),
            EffectsSha256 = Hash(Path.Combine(directory, GenStatemValidationPaths.Effects)),
            FeatureBootstrapSha256 = Hash(Path.Combine(directory, GenStatemValidationPaths.FeatureBootstrap)),
            StartupSelection = runtime.LibrarySelection.Modules.Select(module => new
            {
                module.Module,
                Backend = module.Backend.ToString(),
                module.InputSha256,
                ExportCount = module.Exports.Count,
                NativePrimitiveCount = module.NativePrimitives.Count,
                module.ManagedAssemblies
            }),
            HelperImages = images.Select(image => new { image.Name, image.OriginalBeamSha256 }),
            ManagedSources = Directory.GetFiles(Path.Combine(repository, GenStatemValidationPaths.ManagedSources), GenStatemValidationPaths.SourcePattern)
                .Order(StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(repository, path).Replace(Path.DirectorySeparatorChar, '/'), Hash),
            LoggerPrimaryLevel = GenStatemValidationProtocol.None,
            BootSubjectSha256 = Hash(Path.Combine(repository, GenStatemValidationPaths.BootSubject))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(GenStatemValidationDiagnostics.Summary(
    args[2],
    passed,
    rows.Length,
    effectPassed,
    failures.Count
));
await Run(async context => await runtime.Modules.Call(
    context,
    GenStatemValidationModules.Erlang,
    GenStatemValidationProtocol.Send,
    environment.Pid,
    Term.A(GenStatemValidationProtocol.Done)
));
await environment.Completion.WaitAsync(GenStatemValidationLimits.OperationDeadline);
return failures.Count == 0 && effectsRun != 0 ? GenStatemValidationLimits.SuccessExitCode : GenStatemValidationLimits.FailureExitCode;

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

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
                Term.A(GenStatemValidationProtocol.Uncaught),
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
    Term value = await result.Task.WaitAsync(GenStatemValidationLimits.OperationDeadline);
    await worker.Completion.WaitAsync(GenStatemValidationLimits.OperationDeadline);

    return value;
}

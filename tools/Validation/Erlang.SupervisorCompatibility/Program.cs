using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is < SupervisorValidationLimits.RequiredArguments or > SupervisorValidationLimits.ArgumentsWithScenario || args[2] is not (SupervisorValidationBaseline.ManagedBackend or SupervisorValidationBaseline.BeamBackend))
{
    Console.Error.WriteLine(SupervisorValidationDiagnostics.Usage);

    return SupervisorValidationLimits.UsageExitCode;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == SupervisorValidationBaseline.ManagedBackend;
var temporaryLogs = args.Length == SupervisorValidationLimits.ArgumentsWithScenario && args[3] == SupervisorTemporaryLogProbe.Scenario
    ? new SupervisorTemporaryLogProbe() : null;
string stdlib = Path.Combine(repository, SupervisorValidationPaths.Stdlib);
string kernel = Path.Combine(repository, SupervisorValidationPaths.Kernel);
string erts = Path.Combine(repository, SupervisorValidationPaths.Erts);
SupervisorAdmission.Verify(repository, directory, stdlib);
string[] dependencies = Directory.GetFiles(stdlib, SupervisorValidationPaths.BeamPattern).Concat(Directory.GetFiles(kernel, SupervisorValidationPaths.BeamPattern))
    .Select(Path.GetFileNameWithoutExtension).Select(name => name!).Append(SupervisorValidationModules.Erlang).Append(SupervisorValidationModules.ErtsInternal).Append(SupervisorValidationModules.ErlPrimLoader).Append(SupervisorValidationModules.SupervisorPortCases).Distinct().ToArray();
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(SupervisorModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string[] helpers = ["gen_server", "supervisor_SUITE", "supervisor_1", "supervisor_2", "supervisor_3", "supervisor_4",SupervisorValidationModules.Gen, SupervisorValidationModules.ProcLib, SupervisorValidationModules.Sys, SupervisorValidationModules.Lists, SupervisorValidationModules.Maps, SupervisorValidationModules.Queue, SupervisorValidationModules.Ets, SupervisorValidationModules.Logger, SupervisorValidationModules.LoggerConfig,
    SupervisorValidationModules.LoggerFilters, SupervisorValidationModules.LoggerBackend, SupervisorValidationModules.LoggerServer, SupervisorValidationModules.LoggerProxy, SupervisorValidationModules.Proplists, SupervisorValidationModules.IoLib, SupervisorValidationModules.IoLibFormat, SupervisorValidationModules.IoLibPretty, SupervisorValidationModules.Io, SupervisorValidationModules.ErrorLogger, SupervisorValidationModules.Application, SupervisorValidationModules.ApplicationController,
    SupervisorValidationModules.String, SupervisorValidationModules.Unicode, SupervisorValidationModules.UnicodeUtil, SupervisorValidationModules.Epp, SupervisorValidationModules.ErlScan, SupervisorValidationModules.ErlFeatures, SupervisorValidationModules.Code,
    SupervisorValidationModules.Init, SupervisorValidationModules.ErlPrimLoader, SupervisorValidationModules.CodeServer, SupervisorValidationModules.PrimFile, SupervisorValidationModules.Filename, SupervisorValidationModules.Os, SupervisorValidationModules.GbSets, SupervisorValidationModules.GbTrees, SupervisorValidationModules.File];
var images = new List<PreparedBeamImage>();
await runtime.InitializeModulesAsync(registry =>
{
    SupervisorValidationPrimitives.Register(runtime, registry);
    RuntimeBootstrapFileSystem.Register(runtime, registry);
    temporaryLogs?.Register(registry);
});
foreach (string module in helpers.Concat(managed ? [] : new[] { SupervisorModule.Name }).Concat([SupervisorValidationModules.SupervisorPortCases, SupervisorValidationModules.SupervisorPortSecond]))
{
    Console.WriteLine(SupervisorValidationDiagnostics.PreparePrefix + module);
    string file = File.Exists(Path.Combine(directory, module + SupervisorValidationPaths.BeamExtension)) ? Path.Combine(directory, module + SupervisorValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(stdlib, module + SupervisorValidationPaths.BeamExtension)) ? Path.Combine(stdlib, module + SupervisorValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(kernel, module + SupervisorValidationPaths.BeamExtension)) ? Path.Combine(kernel, module + SupervisorValidationPaths.BeamExtension) : Path.Combine(erts, module + SupervisorValidationPaths.BeamExtension);
    try
    {
        byte[] bytes = File.ReadAllBytes(file);
        images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(dependencies.Concat(SupervisorBeamImports.Read(runtime.Modules, bytes)))));
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
        throw new InvalidOperationException(SupervisorValidationDiagnostics.PublicationFailed);
    }
});
foreach (var export in SupervisorModule.Exports)
{
    if (!runtime.Modules.Exports.Contains((SupervisorModule.Name, export.Name, export.Arity))
        || managed != (runtime.Modules.SourceOwner(SupervisorModule.Name, export.Name, export.Arity) is null))
    {
        throw new InvalidOperationException(SupervisorValidationDiagnostics.BackendOwnerMismatch);
    }
}
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, SupervisorValidationPaths.Corpus))).Term).ToArray();
var environmentReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle environment = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            SupervisorValidationModules.Ets,
            SupervisorValidationProtocol.New,
            Term.A(SupervisorValidationProtocol.ApplicationTable),
            Term.List(Term.A(SupervisorValidationProtocol.NamedTable), Term.A(SupervisorValidationProtocol.Public))
        );
        Term loggerTable = await runtime.Modules.Call(
            context,
            SupervisorValidationModules.LoggerConfig,
            SupervisorValidationProtocol.New,
            Term.A(SupervisorValidationProtocol.LoggerTable)
        );
        Term primary = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(SupervisorValidationProtocol.Level)] = Term.A(temporaryLogs is null ? SupervisorValidationProtocol.None : SupervisorTemporaryLogProbe.Error),
            [Term.A(SupervisorValidationProtocol.Filters)] = temporaryLogs is null ? Nil.Value : await temporaryLogs.Filter(context),
            [Term.A(SupervisorValidationProtocol.FilterDefault)] = Term.A(SupervisorValidationProtocol.Log),
            [Term.A(SupervisorValidationProtocol.Handlers)] = Nil.Value,
            [Term.A(SupervisorValidationProtocol.Metadata)] = new MapTerm([])
        });
        await runtime.Modules.Call(
            context,
            SupervisorValidationModules.LoggerConfig,
            SupervisorValidationProtocol.Create,
            loggerTable,
            Term.A(SupervisorValidationProtocol.Primary),
            primary
        );
        foreach (TupleTerm entry in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, SupervisorValidationPaths.FeatureBootstrap))).Term))
        {
            await runtime.Modules.Call(
                context,
                SupervisorValidationModules.PersistentTerm,
                SupervisorValidationProtocol.Put,
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
await environmentReady.Task.WaitAsync(SupervisorValidationLimits.OperationDeadline);
await Run(context => runtime.Modules.Call(context, SupervisorValidationModules.PrimFile, SupervisorValidationProtocol.OnLoad));
using (var bootDeadline = new CancellationTokenSource(SupervisorValidationLimits.BootDeadline))
{
    RuntimeOtpBootSession boot = await runtime.StartOtpBootAsync(SupervisorCodeBoot.Create(repository, directory), bootDeadline.Token);
    if (boot.Completion.IsCompleted)
    {
        throw new InvalidOperationException(SupervisorValidationDiagnostics.BootEnded);
    }
}
Term[] effects = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, SupervisorValidationPaths.Effects))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
int effectPassed = 0;
int effectsRun = 0;
if (args.Length == SupervisorValidationLimits.RequiredArguments)
{
    foreach (TupleTerm row in rows)
    {
        try
        {
            Term actual = await Run(context => runtime.Modules.Call(
                context,
                SupervisorValidationModules.SupervisorPortCases,
                SupervisorValidationProtocol.Observe,
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
    var expectedLogs = temporaryLogs?.Expected(row.Items[0]);
    if (temporaryLogs is not null ? expectedLogs is null
        : args.Length == SupervisorValidationLimits.ArgumentsWithScenario && args[3] != (row.Items[0] is Atom named ? named.Name : row.Items[0].ToString()))
    {
        continue;
    }
    effectsRun++;
    Console.WriteLine(SupervisorValidationDiagnostics.ScenarioPrefix + row.Items[0]);
    try
    {
        int previousLogs = temporaryLogs?.Reports ?? 0;
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            SupervisorValidationModules.SupervisorPortCases,
            SupervisorValidationProtocol.Scenario,
            row.Items[0]
        ));
        int actualLogs = (temporaryLogs?.Reports ?? 0) - previousLogs;
        if (expectedLogs is not null && expectedLogs != actualLogs)
        {
            failures.Add(new { Scenario = row.Items[0].ToString(), ExpectedReports = expectedLogs, ActualReports = actualLogs });
        }
        else if (actual.Equals(row.Items[1]))
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
        args[2] + (args.Length == SupervisorValidationLimits.ArgumentsWithScenario ? SupervisorValidationPaths.ScenarioSeparator + args[3] : "") + SupervisorValidationPaths.ReportSuffix
    ),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = SupervisorValidationBaseline.Release,
            Exports = SupervisorModule.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            EffectCases = effects.Length,
            EffectPassed = effectPassed,
            EffectsRun = effectsRun,
            Complete = args.Length == SupervisorValidationLimits.RequiredArguments && passed == rows.Length && effectPassed == effects.Length && failures.Count == 0,
            Failures = failures,
            OriginalSha256 = Hash(Path.Combine(stdlib, SupervisorValidationPaths.OriginalBeam)),
            ManagedAssemblySha256 = Hash(typeof(SupervisorModule).Assembly.Location),
            RuntimeAssemblySha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            CorpusSha256 = Hash(Path.Combine(directory, SupervisorValidationPaths.Corpus)),
            EffectsSha256 = Hash(Path.Combine(directory, SupervisorValidationPaths.Effects)),
            FeatureBootstrapSha256 = Hash(Path.Combine(directory, SupervisorValidationPaths.FeatureBootstrap)),
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
            ManagedSources = Directory.GetFiles(Path.Combine(repository, SupervisorValidationPaths.ManagedSources), SupervisorValidationPaths.SourcePattern)
                .Order(StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(repository, path).Replace(Path.DirectorySeparatorChar, '/'), Hash),
            LoggerPrimaryLevel = temporaryLogs is null ? SupervisorValidationProtocol.None : SupervisorTemporaryLogProbe.Error,
            BootSubjectSha256 = Hash(Path.Combine(repository, SupervisorValidationPaths.BootSubject))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(SupervisorValidationDiagnostics.Summary(
    args[2],
    passed,
    rows.Length,
    effectPassed,
    failures.Count
));
await Run(async context => await runtime.Modules.Call(
    context,
    SupervisorValidationModules.Erlang,
    SupervisorValidationProtocol.Send,
    environment.Pid,
    Term.A(SupervisorValidationProtocol.Done)
));
await environment.Completion.WaitAsync(SupervisorValidationLimits.OperationDeadline);
return failures.Count == 0 && effectsRun != 0 ? SupervisorValidationLimits.SuccessExitCode : SupervisorValidationLimits.FailureExitCode;

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
                Term.A(SupervisorValidationProtocol.Uncaught),
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
    Term value = await result.Task.WaitAsync(SupervisorValidationLimits.OperationDeadline);
    await worker.Completion.WaitAsync(SupervisorValidationLimits.OperationDeadline);

    return value;
}

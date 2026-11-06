using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is < GenServerValidationLimits.RequiredArguments or > GenServerValidationLimits.ArgumentsWithScenario || args[2] is not (GenServerValidationBaseline.ManagedBackend or GenServerValidationBaseline.BeamBackend))
{
    Console.Error.WriteLine(GenServerValidationDiagnostics.Usage);

    return GenServerValidationLimits.UsageExitCode;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == GenServerValidationBaseline.ManagedBackend;
string stdlib = Path.Combine(repository, GenServerValidationPaths.Stdlib);
string kernel = Path.Combine(repository, GenServerValidationPaths.Kernel);
string erts = Path.Combine(repository, GenServerValidationPaths.Erts);
GenServerAdmission.Verify(repository, directory, stdlib);
string[] dependencies = Directory.GetFiles(stdlib, GenServerValidationPaths.BeamPattern).Concat(Directory.GetFiles(kernel, GenServerValidationPaths.BeamPattern))
    .Select(Path.GetFileNameWithoutExtension).Select(name => name!).Append(GenServerValidationModules.Erlang).Append(GenServerValidationModules.ErtsInternal).Append(GenServerValidationModules.ErlPrimLoader).Append(GenServerValidationModules.GenServerPortCases).Distinct().ToArray();
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(GenServerModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
ProcessHandle ioDevice = GenServerValidationIoDevice.Start(runtime);
string[] helpers = [GenServerValidationModules.NetKernel, GenServerValidationModules.Gen, GenServerValidationModules.ProcLib, GenServerValidationModules.Sys, GenServerValidationModules.Lists, GenServerValidationModules.Maps, GenServerValidationModules.Queue, GenServerValidationModules.Ets, GenServerValidationModules.Logger, GenServerValidationModules.LoggerConfig,
    GenServerValidationModules.LoggerFilters, GenServerValidationModules.LoggerBackend, GenServerValidationModules.LoggerServer, GenServerValidationModules.LoggerProxy, GenServerValidationModules.Proplists, GenServerValidationModules.IoLib, GenServerValidationModules.IoLibFormat, GenServerValidationModules.IoLibPretty, GenServerValidationModules.Io, GenServerValidationModules.ErrorLogger, GenServerValidationModules.Application, GenServerValidationModules.ApplicationController,
    GenServerValidationModules.String, GenServerValidationModules.Unicode, GenServerValidationModules.UnicodeUtil, GenServerValidationModules.Epp, GenServerValidationModules.ErlScan, GenServerValidationModules.ErlFeatures, GenServerValidationModules.Code,
    GenServerValidationModules.Init, GenServerValidationModules.ErlPrimLoader, GenServerValidationModules.CodeServer, GenServerValidationModules.PrimFile, GenServerValidationModules.Filename, GenServerValidationModules.Os, GenServerValidationModules.GbSets, GenServerValidationModules.GbTrees, GenServerValidationModules.File];
var images = new List<PreparedBeamImage>();
await runtime.InitializeModulesAsync(registry =>
{
    GenServerValidationPrimitives.Register(runtime, registry);
    RuntimeBootstrapFileSystem.Register(runtime, registry);
});
foreach (string module in helpers.Concat(managed ? [] : new[] { GenServerModule.Name }).Concat([GenServerValidationModules.GenServerPortCases, GenServerValidationModules.GenServerPortSecond, GenServerValidationModules.UpstreamSuite]))
{
    Console.WriteLine(GenServerValidationDiagnostics.PreparePrefix + module);
    string file = module is GenServerValidationModules.GenServerPortCases or GenServerValidationModules.GenServerPortSecond or GenServerValidationModules.UpstreamSuite ? Path.Combine(directory, module + GenServerValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(stdlib, module + GenServerValidationPaths.BeamExtension)) ? Path.Combine(stdlib, module + GenServerValidationPaths.BeamExtension)
        : File.Exists(Path.Combine(kernel, module + GenServerValidationPaths.BeamExtension)) ? Path.Combine(kernel, module + GenServerValidationPaths.BeamExtension) : Path.Combine(erts, module + GenServerValidationPaths.BeamExtension);
    try
    {
        byte[] bytes = File.ReadAllBytes(file);
        images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(dependencies.Concat(GenServerBeamImports.Read(runtime.Modules, bytes)))));
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
        throw new InvalidOperationException(GenServerValidationDiagnostics.PublicationFailed);
    }
});
foreach (var export in GenServerModule.Exports)
{
    if (!runtime.Modules.Exports.Contains((GenServerModule.Name, export.Name, export.Arity))
        || managed != (runtime.Modules.SourceOwner(GenServerModule.Name, export.Name, export.Arity) is null))
    {
        throw new InvalidOperationException(GenServerValidationDiagnostics.BackendOwnerMismatch);
    }
}
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenServerValidationPaths.Corpus))).Term).ToArray();
var environmentReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle environment = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            GenServerValidationModules.Ets,
            GenServerValidationProtocol.New,
            Term.A(GenServerValidationProtocol.ApplicationTable),
            Term.List(Term.A(GenServerValidationProtocol.NamedTable), Term.A(GenServerValidationProtocol.Public))
        );
        Term loggerTable = await runtime.Modules.Call(
            context,
            GenServerValidationModules.LoggerConfig,
            GenServerValidationProtocol.New,
            Term.A(GenServerValidationProtocol.LoggerTable)
        );
        Term primary = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(GenServerValidationProtocol.Level)] = Term.A(GenServerValidationProtocol.None),
            [Term.A(GenServerValidationProtocol.Filters)] = Nil.Value,
            [Term.A(GenServerValidationProtocol.FilterDefault)] = Term.A(GenServerValidationProtocol.Log),
            [Term.A(GenServerValidationProtocol.Handlers)] = Nil.Value,
            [Term.A(GenServerValidationProtocol.Metadata)] = new MapTerm([])
        });
        await runtime.Modules.Call(
            context,
            GenServerValidationModules.LoggerConfig,
            GenServerValidationProtocol.Create,
            loggerTable,
            Term.A(GenServerValidationProtocol.Primary),
            primary
        );
        foreach (TupleTerm entry in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenServerValidationPaths.FeatureBootstrap))).Term))
        {
            await runtime.Modules.Call(
                context,
                GenServerValidationModules.PersistentTerm,
                GenServerValidationProtocol.Put,
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
await environmentReady.Task.WaitAsync(GenServerValidationLimits.OperationDeadline);
await Run(context => runtime.Modules.Call(context, GenServerValidationModules.PrimFile, GenServerValidationProtocol.OnLoad));
using (var bootDeadline = new CancellationTokenSource(GenServerValidationLimits.BootDeadline))
{
    RuntimeOtpBootSession boot = await runtime.StartOtpBootAsync(GenServerCodeBoot.Create(repository, directory), bootDeadline.Token);
    if (boot.Completion.IsCompleted)
    {
        throw new InvalidOperationException(GenServerValidationDiagnostics.BootEnded);
    }
}
Term[] effects = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, GenServerValidationPaths.Effects))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
int effectPassed = 0;
int effectsRun = 0;
if (args.Length == GenServerValidationLimits.RequiredArguments)
{
    foreach (TupleTerm row in rows)
    {
        try
        {
            Term actual = await Run(context => runtime.Modules.Call(
                context,
                GenServerValidationModules.GenServerPortCases,
                GenServerValidationProtocol.Observe,
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
    if (args.Length == GenServerValidationLimits.ArgumentsWithScenario && args[3] != (row.Items[0] is Atom named ? named.Name : row.Items[0].ToString()))
    {
        continue;
    }
    effectsRun++;
    Console.WriteLine(GenServerValidationDiagnostics.ScenarioPrefix + row.Items[0]);
    try
    {
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            GenServerValidationModules.GenServerPortCases,
            GenServerValidationProtocol.Scenario,
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
        args[2] + (args.Length == GenServerValidationLimits.ArgumentsWithScenario ? GenServerValidationPaths.ScenarioSeparator + args[3] : "") + GenServerValidationPaths.ReportSuffix
    ),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = GenServerValidationBaseline.Release,
            Exports = GenServerModule.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            EffectCases = effects.Length,
            EffectPassed = effectPassed,
            EffectsRun = effectsRun,
            Complete = args.Length == GenServerValidationLimits.RequiredArguments && passed == rows.Length && effectPassed == effects.Length && failures.Count == 0,
            Failures = failures,
            OriginalSha256 = Hash(Path.Combine(stdlib, GenServerValidationPaths.OriginalBeam)),
            ManagedAssemblySha256 = Hash(typeof(GenServerModule).Assembly.Location),
            RuntimeAssemblySha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            CorpusSha256 = Hash(Path.Combine(directory, GenServerValidationPaths.Corpus)),
            EffectsSha256 = Hash(Path.Combine(directory, GenServerValidationPaths.Effects)),
            FeatureBootstrapSha256 = Hash(Path.Combine(directory, GenServerValidationPaths.FeatureBootstrap)),
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
            ManagedSources = Directory.GetFiles(Path.Combine(repository, GenServerValidationPaths.ManagedSources), GenServerValidationPaths.SourcePattern)
                .Order(StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(repository, path).Replace(Path.DirectorySeparatorChar, '/'), Hash),
            LoggerPrimaryLevel = GenServerValidationProtocol.None,
            BootSubjectSha256 = Hash(Path.Combine(repository, GenServerValidationPaths.BootSubject))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(GenServerValidationDiagnostics.Summary(
    args[2],
    passed,
    rows.Length,
    effectPassed,
    failures.Count
));
await Run(async context => await runtime.Modules.Call(
    context,
    GenServerValidationModules.Erlang,
    GenServerValidationProtocol.Send,
    environment.Pid,
    Term.A(GenServerValidationProtocol.Done)
));
await environment.Completion.WaitAsync(GenServerValidationLimits.OperationDeadline);
return failures.Count == 0 && effectsRun != 0 ? GenServerValidationLimits.SuccessExitCode : GenServerValidationLimits.FailureExitCode;

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
{
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            runtime.SetGroupLeader(ioDevice.Pid, context.Self);
            result.TrySetResult(await body(context));
        }
        catch (ErlangException error)
        {
            result.TrySetResult(Term.Tuple(
                Term.A(GenServerValidationProtocol.Uncaught),
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
    Term value = await result.Task.WaitAsync(GenServerValidationLimits.OperationDeadline);
    await worker.Completion.WaitAsync(GenServerValidationLimits.OperationDeadline);

    return value;
}

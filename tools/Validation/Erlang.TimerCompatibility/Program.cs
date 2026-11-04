using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is not (TimerCompatibilityOptions.RequiredArguments or TimerCompatibilityOptions.MaximumArguments) || args[TimerCompatibilityOptions.Backend] is not (nameof(RuntimeModuleBackend.Managed) or nameof(RuntimeModuleBackend.Beam)))
{
    Console.Error.WriteLine(TimerCompatibilityDiagnostics.Usage);

    return 2;
}
string repository = Path.GetFullPath(args[TimerCompatibilityOptions.Repository]);
string directory = Path.GetFullPath(args[TimerCompatibilityOptions.OracleDirectory]);
bool managed = args[TimerCompatibilityOptions.Backend] == nameof(RuntimeModuleBackend.Managed);
bool suite = args.Length == TimerCompatibilityOptions.MaximumArguments && args[TimerCompatibilityOptions.Mode] == TimerCompatibilityOptions.Suite;
int? depth = args.Length == TimerCompatibilityOptions.MaximumArguments && args[TimerCompatibilityOptions.Mode].StartsWith(TimerCompatibilityOptions.DepthPrefix, StringComparison.Ordinal) ? int.Parse(args[TimerCompatibilityOptions.Mode][TimerCompatibilityOptions.DepthPrefix.Length..]) : null;
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(TimerLibrary.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string stdlib = Path.Combine(repository, TimerCompatibilityFiles.Stdlib);
string kernel = Path.Combine(repository, TimerCompatibilityFiles.Kernel);
string[] dependencies = Directory.GetFiles(stdlib, TimerCompatibilityFiles.BeamPattern).Concat(Directory.GetFiles(kernel, TimerCompatibilityFiles.BeamPattern))
    .Select(file => Path.GetFileNameWithoutExtension(file)).Concat([ErlangModuleNames.Module, TimerCompatibilityNames.CommonTest, TimerCompatibilityNames.Observer, TimerCompatibilityNames.SuiteObserver, TimerCompatibilityNames.UpstreamSuite]).Distinct().ToArray();
string[] helpers = [TimerCompatibilityModules.GenServer, TimerCompatibilityModules.Gen, TimerCompatibilityModules.ProcLib, TimerCompatibilityModules.Sys, TimerCompatibilityModules.Supervisor, TimerCompatibilityModules.Lists, TimerCompatibilityModules.Maps, TimerCompatibilityModules.Queue, TimerCompatibilityModules.Ets,
    TimerCompatibilityModules.Logger, TimerCompatibilityModules.LoggerConfig, TimerCompatibilityModules.LoggerFilters, TimerCompatibilityModules.LoggerBackend, TimerCompatibilityModules.LoggerServer, TimerCompatibilityModules.LoggerProxy, TimerCompatibilityModules.Proplists];
var images = new List<PreparedBeamImage>();
await runtime.InitializeModulesAsync(registry => TimerCompatibilityBootstrap.Register(runtime, registry));
foreach (string module in helpers.Concat(managed ? [] : new[] { TimerLibrary.Name }).Append(TimerCompatibilityNames.Observer)
    .Concat(suite ? [TimerCompatibilityNames.UpstreamSuite, TimerCompatibilityNames.SuiteObserver] : Array.Empty<string>()))
{
    Console.WriteLine(TimerCompatibilityDiagnostics.Preparing + module);
    string file = module is TimerCompatibilityNames.Observer or TimerCompatibilityNames.UpstreamSuite or TimerCompatibilityNames.SuiteObserver ? Path.Combine(directory, module + TimerCompatibilityFiles.BeamExtension)
        : File.Exists(Path.Combine(stdlib, module + TimerCompatibilityFiles.BeamExtension)) ? Path.Combine(stdlib, module + TimerCompatibilityFiles.BeamExtension)
        : Path.Combine(kernel, module + TimerCompatibilityFiles.BeamExtension);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(file), new BeamDependencySelection(dependencies)));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(TimerCompatibilityDiagnostics.Publication);
    }
});
foreach (var export in TimerLibrary.Exports)
{
    if (!runtime.Modules.HasExport(TimerLibrary.Name, export.Name, export.Arity)
        || (runtime.Modules.SourceOwner(TimerLibrary.Name, export.Name, export.Arity) is PreparedBeamImage) == managed)
    {
        throw new InvalidOperationException(TimerCompatibilityDiagnostics.Owner);
    }
}
var oracleExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, TimerCompatibilityFiles.Exports))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
var registeredExports = runtime.Modules.Exports.Where(row => row.Module == TimerLibrary.Name && row.Function != TimerCompatibilityNames.ModuleInfo)
    .Select(row => (row.Function, row.Arity)).ToHashSet();
if (!oracleExports.SetEquals(TimerLibrary.Exports) || !oracleExports.SetEquals(registeredExports))
{
    throw new InvalidOperationException(TimerCompatibilityDiagnostics.Exports);
}
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, TimerCompatibilityFiles.Primary))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
Term[] effects = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, TimerCompatibilityFiles.Effects)))).ToArray();
TupleTerm startup = effects.Cast<TupleTerm>().Single(row => row.Items[0] is Atom { Name: TimerCompatibilityNames.Start });
Term started = await Run(context => runtime.Modules.Call(
    context,
    TimerCompatibilityNames.Observer,
    TimerCompatibilityNames.Scenario,
    startup.Items[0]
));
if (!started.Equals(startup.Items[1]))
{
    throw new InvalidOperationException(TimerCompatibilityDiagnostics.Startup + started);
}
if (depth is { } requestedDepth)
{
    _ = await Run(context => runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        TimerCompatibilityNames.SystemFlag,
        Term.A(TimerCompatibilityNames.BacktraceDepth),
        Term.I(requestedDepth)
    ));
}
if (suite)
{
    Term[] suiteRows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, TimerCompatibilityFiles.Upstream))).Term).ToArray();
    foreach (TupleTerm row in suiteRows)
    {
        if (row.Items[1] is not TupleTerm { Items.Count: 2 } expected || expected.Items[0] is not Atom { Name: TimerCompatibilityNames.Ok })
        {
            throw new InvalidOperationException(TimerCompatibilityDiagnostics.OriginalFailure + row);
        }
        Console.WriteLine(TimerCompatibilityDiagnostics.Upstream + row.Items[0]);
        Term sent = await Run(context => runtime.Modules.Call(
            context,
            TimerCompatibilityNames.SuiteObserver,
            TimerCompatibilityNames.Worker,
            row.Items[0],
            context.Self
        ));
        Term actual = ((TupleTerm)sent).Items[1];
        if (actual.Equals(row.Items[1]))
        {
            passed++;
        }
        else
        {
            failures.Add(new { Test = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
        }
    }
    File.WriteAllText(
        Path.Combine(directory, args[TimerCompatibilityOptions.Backend] + TimerCompatibilityFiles.UpstreamReportSuffix),
        JsonSerializer.Serialize(
            new
            {
                Backend = args[TimerCompatibilityOptions.Backend],
                Cases = suiteRows.Length,
                Passed = passed,
                Failures = failures
            },
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
    Console.WriteLine(TimerCompatibilityReportText.Upstream(args[TimerCompatibilityOptions.Backend], passed, suiteRows.Length));

    return failures.Count == 0 ? 0 : 1;
}
foreach (TupleTerm row in rows)
{
    Term sent = await Run(context => runtime.Modules.Call(
        context,
        TimerCompatibilityNames.Observer,
        TimerCompatibilityNames.Worker,
        row.Items[0],
        row.Items[1],
        context.Self
    ));
    Term result = ((TupleTerm)sent).Items[1];
    if (result.Equals(row.Items[2]))
    {
        passed++;
    }
    else
    {
        failures.Add(new { Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = row.Items[2].ToString(), Actual = result.ToString() });
    }
}
int effectPassed = 0;
foreach (TupleTerm row in effects)
{
    Console.WriteLine(TimerCompatibilityDiagnostics.Scenario + row.Items[0]);
    Term sent = await Run(context => runtime.Modules.Call(
        context,
        TimerCompatibilityNames.Observer,
        TimerCompatibilityNames.ScenarioWorker,
        row.Items[0],
        context.Self
    ));
    Term result = ((TupleTerm)sent).Items[1];
    if (result.Equals(row.Items[1]))
    {
        effectPassed++;
    }
    else
    {
        failures.Add(new { Scenario = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = result.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(directory, args[TimerCompatibilityOptions.Backend] + TimerCompatibilityFiles.ReportSuffix),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[TimerCompatibilityOptions.Backend],
            Baseline = TimerCompatibilityNames.Baseline,
            Exports = TimerLibrary.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            EffectPassed = effectPassed,
            Failures = failures,
            TimerBeamSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(stdlib, TimerCompatibilityFiles.TimerImage)))),
            ManagedAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(TimerLibrary).Assembly.Location))),
            RuntimeAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
            CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, TimerCompatibilityFiles.Primary))))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(TimerCompatibilityReportText.Primary(
    args[TimerCompatibilityOptions.Backend],
    passed,
    rows.Length,
    effectPassed,
    failures.Count
));

return failures.Count == 0 ? 0 : 1;

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
                Term.A(TimerCompatibilityNames.Uncaught),
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
    Term value = await result.Task.WaitAsync(TimerCompatibilityLimits.WorkerDeadline);
    await worker.Completion.WaitAsync(TimerCompatibilityLimits.ExitDeadline);

    return value;
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

if (args.Length is < 3 or > 4 || args[2] is not (nameof(RuntimeModuleBackend.Managed) or nameof(RuntimeModuleBackend.Beam))
    || args.Length == 4 && args[3] != ApplicationValidationFields.ApiOnly)
{
    throw new ArgumentException(ApplicationValidationDiagnostics.Usage);
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
ApplicationAdmission.Verify(repository, directory);
bool managed = args[2] == nameof(RuntimeModuleBackend.Managed);
bool apiOnly = args.Length == 4;
string original = Path.Combine(repository, ApplicationValidationPaths.OriginalFoundationDirectory);
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var inputImages = new Dictionary<string, string>();
var failures = new List<object>();
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(ApplicationModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
await runtime.InitializeModulesAsync(registry =>
{
    GenStatemValidationPrimitives.Register(runtime, registry);
    RuntimeBootstrapFileSystem.Register(runtime, registry);
});
string[] helpers = [ApplicationValidationModules.ApplicationController, ApplicationValidationModules.ApplicationMaster, ApplicationValidationModules.ApplicationStarter, ApplicationValidationModules.DistAc,
    ApplicationValidationModules.Gen, ApplicationValidationModules.GenServer, ApplicationValidationModules.ProcLib, ApplicationValidationModules.Sys, ApplicationValidationModules.Lists, ApplicationValidationModules.Maps, ApplicationValidationModules.Queue, ApplicationValidationModules.Ets, ApplicationValidationModules.Logger, ApplicationValidationModules.LoggerConfig,
    ApplicationValidationModules.LoggerFilters, ApplicationValidationModules.LoggerBackend, ApplicationValidationModules.LoggerServer, ApplicationValidationModules.LoggerProxy, ApplicationValidationModules.Proplists, ApplicationValidationModules.IoLib, ApplicationValidationModules.IoLibFormat,
    ApplicationValidationModules.IoLibPretty, ApplicationValidationModules.Io, ApplicationValidationModules.ErrorLogger, ApplicationValidationModules.String, ApplicationValidationModules.Unicode, ApplicationValidationModules.UnicodeUtil, ApplicationValidationModules.Epp, ApplicationValidationModules.ErlScan, ApplicationValidationModules.ErlFeatures,
    ApplicationValidationModules.Code, ApplicationValidationModules.Init, ApplicationValidationModules.ErlPrimLoader, ApplicationValidationModules.CodeServer, ApplicationValidationModules.PrimFile, ApplicationValidationModules.Filename, ApplicationValidationModules.Os, ApplicationValidationModules.GbSets, ApplicationValidationModules.GbTrees, ApplicationValidationModules.File, ApplicationValidationModules.ErlParse, ApplicationValidationModules.ErlAnno, ApplicationValidationModules.ErlPosixMsg];
string[] dependencies = Directory.EnumerateFiles(original, ApplicationValidationPaths.Beam, SearchOption.AllDirectories)
    .Select(path => Path.GetFileNameWithoutExtension(path)).Append(ApplicationValidationModules.Erlang)
    .Concat([ApplicationValidationModules.ApplicationPortCases, ApplicationValidationModules.ApplicationPortCallback, ApplicationValidationModules.ApplicationSUITE, ApplicationValidationModules.Ct, ApplicationValidationModules.TestServer, ApplicationValidationModules.Peer]).Distinct().ToArray();
var images = new List<PreparedBeamImage>();
foreach (string module in helpers.Concat(managed ? [] : new[] { ApplicationModule.Name }).Concat([ApplicationValidationModules.ApplicationPortCases, ApplicationValidationModules.ApplicationPortCallback, ApplicationValidationModules.ApplicationSUITE]))
{
    string path = module.StartsWith(ApplicationValidationFields.FixturePrefix, StringComparison.Ordinal) || module == ApplicationValidationModules.ApplicationSUITE
        ? Path.Combine(directory, module + ApplicationValidationPaths.BeamExtension)
        : Directory.EnumerateFiles(original, module + ApplicationValidationPaths.BeamExtension, SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + ApplicationValidationPaths.Variants + Path.DirectorySeparatorChar, StringComparison.Ordinal)).Single();
    inputImages[Path.GetRelativePath(repository, path)] = Hash(path);
    Console.WriteLine(ApplicationValidationDiagnostics.Prepare + module);
    byte[] bytes = File.ReadAllBytes(path);
    images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(dependencies.Concat(GenStatemBeamImports.Read(runtime.Modules, bytes)))));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(ApplicationValidationDiagnostics.PublicationRefused);
    }
});
foreach (var export in ApplicationModule.Exports)
{
    if (!runtime.Modules.Exports.Contains((ApplicationModule.Name, export.Name, export.Arity))
        || managed != (runtime.Modules.SourceOwner(ApplicationModule.Name, export.Name, export.Arity) is null))
    {
        throw new InvalidOperationException(ApplicationValidationDiagnostics.BackendMismatch);
    }
}
var expectedExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Exports))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value))
    .Where(row => row.Name != ApplicationValidationProtocol.ModuleInfo).ToHashSet();
if (!expectedExports.SetEquals(ApplicationModule.Exports))
{
    throw new InvalidDataException(ApplicationValidationDiagnostics.ExportMismatch);
}

Task<Term> Run(Func<ProcessContext, ValueTask<Term>> call, bool keepAlive = false)
{
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    runtime.Spawn(async context =>
    {
        try
        {
            completion.TrySetResult(await call(context));
            if (keepAlive)
            {
                await context.ReceiveAsync(message => message);
            }
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });

    return completion.Task.WaitAsync(TimeSpan.FromSeconds(45));
}
await Run(context => runtime.Modules.Call(context, ApplicationValidationModules.PrimFile, ApplicationValidationProtocol.OnLoad));
string bootRoot = Path.Combine(
    directory,
    ApplicationValidationPaths.Boot + args[2] + ApplicationValidationFields.ReportSeparator + Guid.NewGuid().ToString(ApplicationValidationFields.GuidFormat)
);
Directory.CreateDirectory(bootRoot);
Term A(string name) => Term.A(name);
Term S(string text) => Term.String(text);
Term kernel = Term.Tuple(
    A(ApplicationValidationProtocol.Application),
    A(ApplicationValidationProtocol.Kernel),
    Term.List(
        Term.Tuple(A(ApplicationValidationProtocol.Description), S(ApplicationValidationFields.ApplicationValidationKernel)),
        Term.Tuple(A(ApplicationValidationProtocol.Vsn), S(ApplicationValidationFields.BootVersion)),
        Term.Tuple(A(ApplicationValidationProtocol.Env), Nil.Value)
    )
);
Term script = Term.Tuple(
    A(ApplicationValidationProtocol.Script),
    Term.Tuple(S(ApplicationValidationPaths.ApplicationPort), S(ApplicationValidationFields.BootVersion)),
    Term.List(
        Term.Tuple(A(ApplicationValidationProtocol.PreLoaded), Nil.Value),
        Term.Tuple(A(ApplicationValidationProtocol.Path), Term.List(S(bootRoot))),
        Term.Tuple(A(ApplicationValidationProtocol.PrimLoad), Nil.Value),
        Term.Tuple(A(ApplicationValidationProtocol.KernelLoadCompleted)),
        Term.Tuple(
            A(ApplicationValidationProtocol.Apply),
            Term.Tuple(A(ApplicationValidationModules.ErlPrimLoader), A(ApplicationValidationProtocol.SetPath), Term.List(Nil.Value))
        ),
        Term.Tuple(
            A(ApplicationValidationProtocol.KernelProcess),
            A(ApplicationValidationModules.CodeServer),
            Term.Tuple(
                A(ApplicationValidationModules.CodeServer),
                A(ApplicationValidationProtocol.StartLink),
                Term.List(Term.List(S(bootRoot), A(ApplicationValidationProtocol.Embedded)))
            )
        ),
        Term.Tuple(
            A(ApplicationValidationProtocol.KernelProcess),
            A(ApplicationValidationModules.ApplicationController),
            Term.Tuple(A(ApplicationValidationModules.ApplicationController), A(ApplicationValidationProtocol.Start), Term.List(kernel))
        ),
        Term.Tuple(A(ApplicationValidationProtocol.Progress), A(ApplicationValidationProtocol.Started))
    )
);
string bootFile = Path.Combine(bootRoot, ApplicationValidationPaths.ApplicationPort);
File.WriteAllBytes(bootFile + ApplicationValidationPaths.BootExtension, ExternalTermFormat.Encode(script));
using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
{
    RuntimeOtpBootSession boot = await runtime.StartOtpBootAsync(
        new[] { ApplicationValidationProtocol.Root, bootRoot, ApplicationValidationProtocol.Bindir, bootRoot, ApplicationValidationProtocol.Boot, bootFile, ApplicationValidationProtocol.Mode, ApplicationValidationProtocol.Embedded }
            .Select(text => new BitString(Encoding.UTF8.GetBytes(text))),
        deadline.Token
    );
    if (boot.Completion.IsCompleted)
    {
        throw new InvalidOperationException(ApplicationValidationDiagnostics.OriginalBootEnded);
    }
}
await Run(
    async context =>
{
    using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Provenance)));
    int depth = provenance.RootElement.GetProperty(ApplicationValidationFields.BacktraceDepth).GetInt32();
    await runtime.Modules.Call(
        context,
        ApplicationValidationModules.Erlang,
        ApplicationValidationProtocol.SystemFlag,
        A(ApplicationValidationProtocol.BacktraceDepth),
        Term.I(depth)
    );
    await runtime.Modules.Call(
        context,
        ApplicationValidationModules.PersistentTerm,
        ApplicationValidationProtocol.Put,
        A(ApplicationValidationProtocol.ApplicationPortPrerequisites),
        runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Prerequisites))).Term
    );
    foreach (TupleTerm entry in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.FeatureBootstrap))).Term))
    {
        await runtime.Modules.Call(
            context,
            ApplicationValidationModules.PersistentTerm,
            ApplicationValidationProtocol.Put,
            entry.Items[0],
            entry.Items[1]
        );
    }
    Term table = await runtime.Modules.Call(
        context,
        ApplicationValidationModules.LoggerConfig,
        ApplicationValidationProtocol.New,
        A(ApplicationValidationModules.Logger)
    );
    Term primary = new MapTerm(new Dictionary<Term, Term>
    {
        [A(ApplicationValidationProtocol.Level)] = A(ApplicationValidationProtocol.None),
        [A(ApplicationValidationProtocol.Filters)] = Nil.Value,
        [A(ApplicationValidationProtocol.FilterDefault)] = A(ApplicationValidationProtocol.Log),
        [A(ApplicationValidationProtocol.Handlers)] = Nil.Value,
        [A(ApplicationValidationProtocol.Metadata)] = new MapTerm([])
    });

    return await runtime.Modules.Call(
        context,
        ApplicationValidationModules.LoggerConfig,
        ApplicationValidationProtocol.Create,
        table,
        A(ApplicationValidationProtocol.Primary),
        primary
    );
},
    true
);
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Corpus))).Term).ToArray();
await Run(context => runtime.Modules.Call(
    context,
    ApplicationValidationModules.Code,
    ApplicationValidationProtocol.AddPatha,
    S(Path.Combine(repository, ApplicationValidationPaths.UpstreamDirectory))
));
int passed = 0;
foreach (TupleTerm row in rows)
{
    Console.WriteLine(ApplicationValidationDiagnostics.Observe + row.Items[0] + ApplicationValidationFields.DiagnosticSpacing + row.Items[1]);
    try
    {
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            ApplicationValidationModules.ApplicationPortCases,
            ApplicationValidationProtocol.Observe,
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
        failures.Add(new { Function = row.Items[0].ToString(), Failure = error.ToString(), Stack = (error as ErlangException)?.StackTraceTerm.ToString() });
    }
}
Term[] effects = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Effects))).Term).ToArray();
int effectPassed = 0;
foreach (TupleTerm row in apiOnly ? [] : effects)
{
    Console.WriteLine(ApplicationValidationDiagnostics.Scenario + row.Items[0]);
    try
    {
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            ApplicationValidationModules.ApplicationPortCases,
            ApplicationValidationProtocol.Scenario,
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
        failures.Add(new { Scenario = row.Items[0].ToString(), Failure = error.ToString(), Stack = (error as ErlangException)?.StackTraceTerm.ToString() });
    }
}
string report = Path.Combine(
    directory,
    args[2] + ApplicationValidationFields.ReportSeparator + Guid.NewGuid().ToString(ApplicationValidationFields.GuidFormat) + ApplicationValidationPaths.Json
);
File.WriteAllText(
    report,
    JsonSerializer.Serialize(
        new
        {
            Baseline = ApplicationValidationBaseline.ReleaseTag,
            Commit = ApplicationValidationBaseline.Commit,
            Backend = args[2],
            ApiOnly = apiOnly,
            Exports = ApplicationModule.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            Scenarios = effects.Length,
            ScenariosPassed = effectPassed,
            Failures = failures,
            Inputs = inputImages,
            Sources = Directory.GetFiles(
                Path.Combine(repository, ApplicationValidationPaths.ImplementationDirectory),
                ApplicationValidationPaths.Cs
            ).ToDictionary(path => Path.GetRelativePath(repository, path), Hash),
            Assemblies = new[] { typeof(ApplicationModule).Assembly, typeof(ProcessRuntime).Assembly }.Select(assembly => new
            {
                assembly.FullName,
                assembly.ManifestModule.ModuleVersionId,
                Sha256 = Hash(assembly.Location)
            })
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(report);
Console.WriteLine($"{args[2]}: {passed}/{rows.Length}, scenarios {effectPassed}/{effects.Length}");

return failures.Count == 0 ? 0 : 1;

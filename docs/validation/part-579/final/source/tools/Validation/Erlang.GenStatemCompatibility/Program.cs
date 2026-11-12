using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is < 3 or > 4 || args[2] is not ("Managed" or "Beam"))
{
    Console.Error.WriteLine(GenStatemValidationDiagnostics.Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == "Managed";
string stdlib = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
string kernel = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin");
string erts = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam/Otp/ebin");
GenStatemAdmission.Verify(repository, directory, stdlib);
string[] dependencies = Directory.GetFiles(stdlib, "*.beam").Concat(Directory.GetFiles(kernel, "*.beam"))
    .Select(Path.GetFileNameWithoutExtension).Select(name => name!).Append("erlang").Append("erts_internal").Append("erl_prim_loader").Append("gen_statem_port_cases").Distinct().ToArray();
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(GenStatemModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string[] helpers = ["gen", "proc_lib", "sys", "lists", "maps", "queue", "ets", "logger", "logger_config",
    "logger_filters", "logger_backend", "logger_server", "logger_proxy", "proplists", "io_lib", "io_lib_format", "io_lib_pretty", "io", "error_logger", "application", "application_controller",
    "string", "unicode", "unicode_util", "epp", "erl_scan", "erl_features", "code",
    "init", "erl_prim_loader", "code_server", "prim_file", "filename", "os", "gb_sets", "gb_trees", "file"];
var images = new List<PreparedBeamImage>();
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeBootstrapEtsLibrary.Register(runtime, registry);
    RuntimeBootstrapFileSystem.Register(runtime, registry);
});
foreach (string module in helpers.Concat(managed ? [] : new[] { GenStatemModule.Name }).Concat(["gen_statem_port_cases", "gen_statem_port_second"]))
{
    Console.WriteLine("Prepare: " + module);
    string file = module is "gen_statem_port_cases" or "gen_statem_port_second" ? Path.Combine(directory, module + ".beam")
        : File.Exists(Path.Combine(stdlib, module + ".beam")) ? Path.Combine(stdlib, module + ".beam")
        : File.Exists(Path.Combine(kernel, module + ".beam")) ? Path.Combine(kernel, module + ".beam") : Path.Combine(erts, module + ".beam");
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
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "original.etf"))).Term).ToArray();
var environmentReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle environment = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            "ets",
            "new",
            Term.A("ac_tab"),
            Term.List(Term.A("named_table"), Term.A("public"))
        );
        Term loggerTable = await runtime.Modules.Call(
            context,
            "logger_config",
            "new",
            Term.A("logger")
        );
        Term primary = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A("level")] = Term.A("none"),
            [Term.A("filters")] = Nil.Value,
            [Term.A("filter_default")] = Term.A("log"),
            [Term.A("handlers")] = Nil.Value,
            [Term.A("metadata")] = new MapTerm([])
        });
        await runtime.Modules.Call(
            context,
            "logger_config",
            "create",
            loggerTable,
            Term.A("primary"),
            primary
        );
        foreach (TupleTerm entry in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "feature-bootstrap.etf"))).Term))
        {
            await runtime.Modules.Call(
                context,
                "persistent_term",
                "put",
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
await environmentReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
await Run(context => runtime.Modules.Call(context, "prim_file", "on_load"));
using (var bootDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
{
    RuntimeOtpBootSession boot = await runtime.StartOtpBootAsync(GenStatemCodeBoot.Create(repository, directory), bootDeadline.Token);
    if (boot.Completion.IsCompleted)
    {
        throw new InvalidOperationException(GenStatemValidationDiagnostics.BootEnded);
    }
}
Term[] effects = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "effects.etf"))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
int effectPassed = 0;
int effectsRun = 0;
if (args.Length == 3)
{
    foreach (TupleTerm row in rows)
    {
        try
        {
            Term actual = await Run(context => runtime.Modules.Call(
                context,
                "gen_statem_port_cases",
                "observe",
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
    if (args.Length == 4 && args[3] != (row.Items[0] is Atom named ? named.Name : row.Items[0].ToString()))
    {
        continue;
    }
    effectsRun++;
    Console.WriteLine("Scenario: " + row.Items[0]);
    try
    {
        Term actual = await Run(context => runtime.Modules.Call(
            context,
            "gen_statem_port_cases",
            "scenario",
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
    Path.Combine(directory, args[2] + (args.Length == 4 ? "-" + args[3] : "") + "-report.json"),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = "OTP-29.1.1",
            Exports = GenStatemModule.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            EffectCases = effects.Length,
            EffectPassed = effectPassed,
            EffectsRun = effectsRun,
            Complete = args.Length == 3 && passed == rows.Length && effectPassed == effects.Length && failures.Count == 0,
            Failures = failures,
            OriginalSha256 = Hash(Path.Combine(stdlib, "gen_statem.beam")),
            ManagedAssemblySha256 = Hash(typeof(GenStatemModule).Assembly.Location),
            RuntimeAssemblySha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            CorpusSha256 = Hash(Path.Combine(directory, "original.etf")),
            EffectsSha256 = Hash(Path.Combine(directory, "effects.etf")),
            FeatureBootstrapSha256 = Hash(Path.Combine(directory, "feature-bootstrap.etf")),
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
            ManagedSources = Directory.GetFiles(Path.Combine(repository, "src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenStatem"), "*.cs")
                .Order(StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(repository, path).Replace(Path.DirectorySeparatorChar, '/'), Hash),
            LoggerPrimaryLevel = "none",
            BootSubjectSha256 = Hash(Path.Combine(repository, "tests/Erlang.Tests/Resources/CodeServerOriginal410/beam_code_server_subject410.beam"))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{args[2]}: {passed}/{rows.Length} contracts; {effectPassed} scenarios; {failures.Count} failures");
await Run(async context => await runtime.Modules.Call(
    context,
    "erlang",
    "send",
    environment.Pid,
    Term.A("done")
));
await environment.Completion.WaitAsync(TimeSpan.FromSeconds(10));
return failures.Count == 0 && effectsRun != 0 ? 0 : 1;

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
                Term.A("uncaught"),
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
    Term value = await result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    await worker.Completion.WaitAsync(TimeSpan.FromSeconds(10));

    return value;
}

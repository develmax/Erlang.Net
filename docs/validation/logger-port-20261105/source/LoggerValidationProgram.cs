using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

if (args.Length != 3 || args[2] is not ("Managed" or "Beam"))
{
    Console.Error.WriteLine("Usage: Erlang.LoggerCompatibility REPOSITORY CORPUS Managed|Beam");

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == "Managed";
string original = Path.Combine(repository, "src/Otp/Original");
string Find(string module) => Directory.EnumerateFiles(original, module + ".beam", SearchOption.AllDirectories).First();
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
using JsonDocument corpusManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "corpus-manifest.json")));
JsonElement manifest = corpusManifest.RootElement;
if (manifest.GetProperty("Baseline").GetString() != "OTP-29.1.1"
    || manifest.GetProperty("Commit").GetString() != "ad05823719d77c8faee87348ea39513d4e2f99c5")
{
    throw new InvalidOperationException("Unadmitted Logger corpus baseline.");
}
foreach (string category in new[] { "Files", "Sources" })
{
    foreach (JsonProperty property in manifest.GetProperty(category).EnumerateObject())
    {
        if (Hash(Path.Combine(category == "Files" ? directory : repository, property.Name)) != property.Value.GetString())
        {
            throw new InvalidOperationException("Logger corpus input changed: " + property.Name);
        }
    }
}
int? backtraceDepth = manifest.GetProperty("BacktraceDepth").ValueKind == JsonValueKind.Number
    ? manifest.GetProperty("BacktraceDepth").GetInt32() : null;
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(LoggerModule.Name)] : []),
    RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.EtsStorageCore,
    CancellationToken.None
);
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeBootstrapFileSystem.Register(runtime, registry);
    GenStatemValidationPrimitives.Register(runtime, registry);
    Erlang.Otp.OtpModules.Register(registry);
});
string[] modules = ["logger_server", "logger_config", "logger_backend", "logger_proxy", "logger_filters", "logger_formatter",
    "logger_simple_h", "logger_std_h", "logger_h_common", "logger_olp", "logger_handler_watcher", "logger_sup", "logger_disk_log_h",
    "gen", "gen_server", "proc_lib", "sys", "application", "application_controller", "application_master", "application_starter", "kernel",
    "io", "io_lib", "io_lib_format", "io_lib_fread", "io_lib_pretty", "calendar", "unicode", "unicode_util", "string", "code", "code_server", "init", "erl_prim_loader",
    "prim_file", "os", "filename", "file", "gb_sets", "gb_trees", "supervisor", "erl_scan", "epp", "erl_features", "erl_lint", "erl_parse",
    "ets", "maps", "lists", "queue", "proplists", "erl_anno", "erl_posix_msg", "dist_ac", "net_kernel"];
string[] dependencies = Directory.EnumerateFiles(original, "*.beam", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension)
    .Concat(runtime.Modules.Exports.Select(value => value.Module)).Concat(["logger_port_cases", "logger_SUITE", "logger_test_lib"]).Distinct().ToArray()!;
var inputs = new Dictionary<string, string>();
var images = new List<PreparedBeamImage>();
foreach (string module in (managed ? modules : modules.Prepend(LoggerModule.Name)).Distinct())
{
    string path = Find(module);
    inputs[Path.GetRelativePath(repository, path)] = Hash(path);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies)));
}
images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, "logger_port_cases.beam")), new BeamDependencySelection(dependencies)));
images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, "logger_SUITE.beam")), new BeamDependencySelection(dependencies)));
await runtime.InitializeModulesAsync(registry =>
{
    foreach (PreparedBeamImage image in images)
    {
        BeamPublicationResult publication = registry.PublishPreparedBeam(image);
        if (publication.Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(image.Name + ": " + publication.Status);
        }
    }
});
foreach (var export in LoggerModule.Exports)
{
    if (!runtime.Modules.HasExport(LoggerModule.Name, export.Name, export.Arity))
    {
        throw new InvalidOperationException("Logger export is missing: " + export);
    }
    object? owner = runtime.Modules.SourceOwner(LoggerModule.Name, export.Name, export.Arity);
    if (managed ? owner is not null : owner is not PreparedBeamImage image || image.OriginalBeamSha256 != Hash(Find(LoggerModule.Name)))
    {
        throw new InvalidOperationException("Logger backend owner mismatch: " + export);
    }
}
Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf")));
if (!LoggerModule.Exports.ToHashSet().SetEquals(Cons.Items(declared).Cast<TupleTerm>()
    .Select(tuple => (((Atom)tuple.Items[0]).Name, (int)((Integer)tuple.Items[1]).Value))))
{
    throw new InvalidOperationException("Logger export set differs from OTP.");
}
// Real OTP init/code_server boot; no replacement logger/configuration server.
string bootRoot = Path.Combine(directory, "boot-" + args[2] + "-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(bootRoot);
Term A(string name) => Term.A(name);
Term kernel = Term.Tuple(
    A("application"),
    A("kernel"),
    Term.List(
        Term.Tuple(A("description"), Term.String("Logger validation kernel")),
        Term.Tuple(A("vsn"), Term.String("1")),
        Term.Tuple(A("env"), Nil.Value)
    )
);
Term script = Term.Tuple(
    A("script"),
    Term.Tuple(Term.String("logger"), Term.String("1")),
    Term.List(
        Term.Tuple(A("preLoaded"), Nil.Value),
        Term.Tuple(A("path"), Term.List(Term.String(bootRoot))),
        Term.Tuple(A("primLoad"), Nil.Value),
        Term.Tuple(A("kernel_load_completed")),
        Term.Tuple(A("apply"), Term.Tuple(A("erl_prim_loader"), A("set_path"), Term.List(Nil.Value))),
        Term.Tuple(
            A("kernelProcess"),
            A("code_server"),
            Term.Tuple(A("code_server"), A("start_link"), Term.List(Term.List(Term.String(bootRoot), A("embedded"))))
        ),
        Term.Tuple(A("kernelProcess"), A("application_controller"), Term.Tuple(A("application_controller"), A("start"), Term.List(kernel))),
        Term.Tuple(A("progress"), A("started"))
    )
);
string boot = Path.Combine(bootRoot, "logger");
File.WriteAllBytes(boot + ".boot", ExternalTermFormat.Encode(script));
var setup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var ownerActor = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(context, "prim_file", "on_load");
        setup.SetResult();
    }
    catch (Exception error)
    {
        setup.SetException(error);
    }

    return Nil.Value;
});
await setup.Task.WaitAsync(TimeSpan.FromSeconds(45));
await ownerActor.Completion;
using var bootCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45));
await runtime.StartOtpBootAsync(
    new[] { "-root", bootRoot, "-bindir", bootRoot, "-boot", boot, "-mode", "embedded" }
    .Select(value => new BitString(Encoding.UTF8.GetBytes(value))),
    bootCancellation.Token
);
var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var started = runtime.Spawn(async context =>
{
    try
    {
        Term result = await runtime.Modules.Call(context, "logger_server", "start_link");
        if (result is not TupleTerm { Items: [Atom { Name: "ok" }, Pid] })
        {
            throw new InvalidOperationException(result.ToString());
        }
        await runtime.Modules.Call(
            context,
            "logger",
            "remove_handler",
            A("simple")
        );
        await runtime.Modules.Call(
            context,
            "logger",
            "set_primary_config",
            new MapTerm([])
        );
        Term supervision = await runtime.Modules.Call(context, "logger_sup", "start_link");
        if (supervision is not TupleTerm { Items: [Atom { Name: "ok" }, Pid] })
        {
            throw new InvalidOperationException(supervision.ToString());
        }
        if (backtraceDepth is int depth)
        {
            await runtime.Modules.Call(
                context,
                "erlang",
                "system_flag",
                A("backtrace_depth"),
                Term.I(depth)
            );
        }
        ready.SetResult();
        await context.ReceiveAsync<Term>(message => message);
    }
    catch (Exception error)
    {
        ready.TrySetException(error);
    }

    return Nil.Value;
});
await ready.Task.WaitAsync(TimeSpan.FromSeconds(45));
var failures = new List<object>();
var actualRows = new List<Term>();
int passed = 0;
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "cases.etf"))).Term).ToArray();
foreach (TupleTerm row in rows)
{
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle process = runtime.Spawn(async context =>
    {
        try
        {
            Term value = await runtime.Modules.Call(
                context,
                "logger_port_cases",
                "capture",
                row.Items[0]
            );
            completion.SetResult(value);
        }
        catch (Exception error)
        {
            completion.SetException(error);
        }

        return Nil.Value;
    });
    try
    {
        Term actual = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        actualRows.Add(Term.Tuple(row.Items[0], actual));
        if (actual.Equals(row.Items[1]))
        {
            passed++;
        }
        else
        {
            failures.Add(new { Case = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
        }
    }
    catch (Exception error)
    {
        failures.Add(new { Case = row.Items[0].ToString(), Error = error.ToString() });
    }
    Console.WriteLine($"{row.Items[0]}: {passed}/{rows.Length}, failures {failures.Count}");
}
string report = Path.Combine(directory, "result-" + args[2] + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json");
string outcomes = Path.ChangeExtension(report, ".etf");
File.WriteAllBytes(outcomes, ExternalTermFormat.Encode(Cons.From(actualRows), new EtfEncodingOptions { FunctionOwner = started.Pid }));
File.WriteAllText(
    report,
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            BacktraceDepth = backtraceDepth,
            CorpusManifestSha256 = Hash(Path.Combine(directory, "corpus-manifest.json")),
            OutcomesSha256 = Hash(outcomes),
            ExportCount = LoggerModule.Exports.Count,
            Attempted = rows.Length,
            Passed = passed,
            Failures = failures,
            Inputs = inputs,
            Sources = Directory.EnumerateFiles(Path.Combine(repository, "src/Otp/Managed/Erlang.Otp.Kernel/Logging"), "Logger*.cs")
        .ToDictionary(path => Path.GetRelativePath(repository, path), Hash),
            Assemblies = new[] { typeof(LoggerModule).Assembly.Location, typeof(ProcessRuntime).Assembly.Location }.ToDictionary(path => path, Hash)
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine(report);

return failures.Count == 0 ? 0 : 1;

using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == nameof(RuntimeModuleBackend.Managed);
bool suite = args.Length > 3 && args[3] == "suite";
string? filter = args.Length > 3 && !suite ? args[3] : null;
string observer = suite ? "error_logger_suite_cases" : "error_logger_port_cases";
string corpus = suite ? "upstream.etf" : "original.etf";
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var sources = Directory.GetFiles(Path.Combine(repository, "src/Otp/Managed/Erlang.Otp.Kernel/Logging/ErrorLogger"), "*.cs")
    .ToDictionary(path => Path.GetRelativePath(repository, path), Hash);
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(ErrorLoggerModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeBootstrapFileSystem.Register(runtime, registry);
    TimerCompatibilityBootstrap.Register(runtime, registry);
});
string original = Path.Combine(repository, "src/Otp/Original");
var files = Directory.GetFiles(original, "*.beam", SearchOption.AllDirectories)
    .GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(group => group.Key!, group => group.First());
string[] modules = ["gen_event", "gen_server", "gen", "proc_lib", "sys", "supervisor", "lists", "maps", "ets", "queue", "proplists",
    "logger", "logger_config", "logger_filters", "logger_backend", "logger_server", "logger_proxy", "logger_sup",
    "logger_handler_watcher", "logger_olp", "logger_simple_h", "logger_std_h", "logger_h_common", "logger_formatter",
    "calendar", "application", "application_controller", "application_master", "application_starter", "io", "io_lib", "io_lib_format", "io_lib_pretty",
    "unicode", "unicode_util", "string", "code", "code_server", "epp", "erl_scan", "erl_features", "init", "erl_prim_loader",
    "prim_file", "prim_buffer", "os", "filename", "gb_sets", "gb_trees", "file", "file_server", "file_io_server", "raw_file_io", "raw_file_io_list", "raw_file_io_delayed", "raw_file_io_compressed", "raw_file_io_deflate", "raw_file_io_inflate", "error_logger_file_h", "error_logger_tty_h", "ct", "test_server", "test_server_sup"];
string[] dependencies = files.Keys.Concat(["erlang", "error_logger_port_cases", "error_logger_SUITE", "test_server", "ct"]).ToArray();
var inputs = new Dictionary<string, string>();
var images = new List<PreparedBeamImage>();
foreach (string module in modules.Concat(managed ? [] : new[] { ErrorLoggerModule.Name }).Append("error_logger_port_cases").Concat(suite ? ["error_logger_suite_cases", "error_logger_SUITE"] : Array.Empty<string>()))
{
    string path = module is "error_logger_port_cases" or "error_logger_suite_cases" or "error_logger_SUITE" ? Path.Combine(directory, module + ".beam") : files[module];
    inputs[Path.GetRelativePath(repository, path)] = Hash(path);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies)));
}
await runtime.InitializeModulesAsync(registry =>
{
    var result = registry.PublishPreparedBeam(images.ToArray());
    if (result.Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(result.Status.ToString());
    }
});
await GenEventBoot.Start(runtime, directory);
Console.WriteLine("BOOT");
Console.WriteLine(await Run(context => runtime.Modules.Call(context, "error_logger_port_cases", "boot")));
Console.WriteLine(await Run(context => runtime.Modules.Call(
    context,
    "logger",
    "remove_handler",
    Term.A("simple")
)));
Console.WriteLine(await Run(context => runtime.Modules.Call(
    context,
    "logger",
    "set_primary_config",
    Term.A("level"),
    Term.A("all")
)));
var exports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf"))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
if (!exports.SetEquals(ErrorLoggerModule.Exports))
{
    throw new InvalidOperationException("Export inventory differs");
}
foreach (var export in ErrorLoggerModule.Exports)
{
    bool beamOwner = runtime.Modules.SourceOwner(ErrorLoggerModule.Name, export.Name, export.Arity) is PreparedBeamImage;
    if (!runtime.Modules.HasExport(ErrorLoggerModule.Name, export.Name, export.Arity) || beamOwner == managed)
    {
        throw new InvalidOperationException("Backend owner differs: " + export);
    }
}
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, corpus))).Term).ToArray();
var failures = new List<object>();
int passed = 0;
int attempted = 0;
foreach (TupleTerm row in rows)
{
    string name = row.Items[0].ToString()!;
    if (filter is not null && !name.Contains(filter, StringComparison.Ordinal))
    {
        continue;
    }
    attempted++;
    Console.WriteLine("RUN " + name);
    try
    {
        var sent = (TupleTerm)await Run(context => runtime.Modules.Call(
            context,
            observer,
            "worker",
            row.Items[0],
            context.Self
        ));
        Term actual = sent.Items[1];
        if (actual.Equals(row.Items[1]))
        {
            passed++;
        }
        else
        {
            failures.Add(new { Case = name, Expected = row.Items[1].ToString(), Actual = actual.ToString() });
            Console.WriteLine("DIFF expected " + row.Items[1] + " actual " + actual);
        }
    }
    catch (Exception error)
    {
        failures.Add(new { Case = name, Error = error.ToString() });
        break;
    }
}
bool registrationPreserved;
await using (ProcessRuntime registrationRuntime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(ErrorLoggerModule.Name)] : [])))
{
    if (!managed)
    {
        PreparedBeamImage selectedBeam = registrationRuntime.Modules.PrepareBeam(
            File.ReadAllBytes(files[ErrorLoggerModule.Name]), new BeamDependencySelection(dependencies));
        await registrationRuntime.InitializeModulesAsync(registry =>
        {
            if (registry.PublishPreparedBeam(selectedBeam).Status != BeamPublicationStatus.Published)
            {
                throw new InvalidOperationException("Registration fixture publication failed");
            }
        });
    }
    ModuleCodeImage? selectedImage = registrationRuntime.Modules.CurrentCodeImage(ErrorLoggerModule.Name);
    await registrationRuntime.InitializeModulesAsync(Erlang.Otp.KernelModules.Register);
    registrationPreserved = ReferenceEquals(selectedImage, registrationRuntime.Modules.CurrentCodeImage(ErrorLoggerModule.Name));
}
if (!registrationPreserved)
{
    throw new InvalidOperationException("Legacy registration displaced error_logger selection");
}
foreach (var input in inputs.Concat(sources))
{
    if (Hash(Path.Combine(repository, input.Key)) != input.Value)
    {
        throw new InvalidOperationException("Input changed: " + input.Key);
    }
}
File.WriteAllText(
    Path.Combine(directory, args[2] + (suite ? "-suite" : filter is null ? "" : "-" + filter) + "-report.json"),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            LegacyRegistrationPreserved = registrationPreserved,
            Exports = exports.Count,
            Cases = rows.Length,
            Attempted = attempted,
            Passed = passed,
            Failures = failures,
            CompleteCorpus = filter is null && attempted == rows.Length,
            Inputs = inputs,
            Sources = sources,
            Assemblies = new[] { typeof(ErrorLoggerModule).Assembly.Location, typeof(ProcessRuntime).Assembly.Location, typeof(Term).Assembly.Location }
            .ToDictionary(path => Path.GetFileName(path)!, Hash),
            CorpusSha256 = Hash(Path.Combine(directory, corpus))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{passed}/{attempted}");
return failures.Count == 0 ? 0 : 1;

async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
{
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle process = runtime.Spawn(async context =>
    {
        try
        {
            completion.TrySetResult(await body(context));
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });
    Term result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));

    return result;
}

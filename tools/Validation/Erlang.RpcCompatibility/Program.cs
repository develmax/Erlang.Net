using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using Erlang.Otp;

string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == "Managed";
if (args.Length > 3 && args[3] == "Connected")
{
    await RpcConnectedChecks.Run(repository, directory, managed);

    return 0;
}
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(RpcLibrary.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string stdlib = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
string kernel = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin");
string[] dependencies = Directory.GetFiles(Path.Combine(repository, "src/Otp/Original"), "*.beam", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension).Cast<string>().Append("erlang").Append("rpc_port_cases").Append("rpc_SUITE").Distinct().ToArray();
await runtime.InitializeModulesAsync(registry => RpcCompatibilityBootstrap.Register(runtime, registry));
string[] helpers = ["net_kernel", "erpc", "gen_server", "gen", "proc_lib", "sys", "supervisor", "lists", "maps", "queue", "ets",
    "logger", "logger_config", "logger_filters", "logger_backend", "logger_server", "logger_proxy", "proplists", "unicode"];
var images = new List<PreparedBeamImage>();
foreach (string module in helpers.Concat(managed ? [] : new[] { "rpc" }).Append("rpc_port_cases").Append("rpc_SUITE"))
{
    string file = module is "rpc_port_cases" or "rpc_SUITE" ? Path.Combine(directory, module + ".beam")
        : File.Exists(Path.Combine(stdlib, module + ".beam")) ? Path.Combine(stdlib, module + ".beam") : Path.Combine(kernel, module + ".beam");
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(file), new BeamDependencySelection(dependencies)));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(RpcCompatibilityDiagnostics.RpcDependencyPublicationFailed);
    }
});
var expectedExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf"))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
var actualExports = runtime.Modules.Exports.Where(row => row.Module == RpcLibrary.Name && row.Function != "module_info")
    .Select(row => (row.Function, row.Arity)).ToHashSet();
if (!expectedExports.SetEquals(RpcLibrary.Exports) || !expectedExports.SetEquals(actualExports))
{
    throw new InvalidOperationException(RpcCompatibilityDiagnostics.PinnedRpcExportsDiffer);
}
foreach (var export in RpcLibrary.Exports)
{
    if (!runtime.Modules.HasExport("rpc", export.Name, export.Arity)
        || (runtime.Modules.SourceOwner("rpc", export.Name, export.Arity) is PreparedBeamImage) == managed)
    {
        throw new InvalidOperationException(RpcCompatibilityDiagnostics.SelectedRpcOwnerDiffers);
    }
}
if (args.Length > 3)
{
    await Run(context => runtime.Modules.Call(
        context,
        "erlang",
        "system_flag",
        Term.A("backtrace_depth"),
        Term.I(int.Parse(args[3]))
    ));
}
Term started = await Run(context => runtime.Modules.Call(context, "rpc", "start"));
Console.WriteLine("RPC start: " + started);
var failures = new List<object>();
int passed = 0;
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "original.etf"))).Term).ToArray();
foreach (TupleTerm row in rows)
{
    Term sent = await Run(context => runtime.Modules.Call(
        context,
        "rpc_port_cases",
        "worker",
        row.Items[0],
        row.Items[1],
        context.Self
    ));
    Term actual = ((TupleTerm)sent).Items[1];
    if (actual.Equals(row.Items[2]))
    {
        passed++;
    }
    else
    {
        failures.Add(new { Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = row.Items[2].ToString(), Actual = actual.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(directory, args[2] + "-primary.json"),
    JsonSerializer.Serialize(new { Cases = rows.Length, Passed = passed, Failures = failures }, new JsonSerializerOptions { WriteIndented = true })
);
int scenariosPassed = 0;
Term[] scenarios = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "effects.etf"))).Term).ToArray();
foreach (TupleTerm row in scenarios)
{
    Console.WriteLine("Scenario: " + row.Items[0]);
    Term sent = await Run(context => runtime.Modules.Call(
        context,
        "rpc_port_cases",
        "scenario_worker",
        row.Items[0],
        context.Self
    ));
    Term actual = ((TupleTerm)sent).Items[1];
    if (actual.Equals(row.Items[1]))
    {
        scenariosPassed++;
    }
    else
    {
        failures.Add(new { Scenario = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(directory, args[2] + "-report.json"),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Exports = RpcLibrary.Exports.Count,
            Cases = rows.Length,
            Passed = passed,
            Scenarios = scenarios.Length,
            ScenariosPassed = scenariosPassed,
            Failures = failures,
            RpcBeamSha256 = Hash(Path.Combine(kernel, "rpc.beam")),
            AssemblySha256 = Hash(typeof(RpcLibrary).Assembly.Location),
            StdlibSha256 = Hash(typeof(ManagedStdlibRegistration456).Assembly.Location),
            RuntimeSha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            CorpusSha256 = Hash(Path.Combine(directory, "original.etf"))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{args[2]}: {passed}/{rows.Length}, scenarios {scenariosPassed}/{scenarios.Length}");
return failures.Count == 0 ? 0 : 1;

static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));

async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
{
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await body(context));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term value = await result.Task.WaitAsync(TimeSpan.FromSeconds(30));
    await worker.Completion.WaitAsync(TimeSpan.FromSeconds(5));

    return value;
}

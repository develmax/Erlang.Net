using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Erlang.Tests.ResourceBoot540Names;

namespace Erlang.Tests;

// Development-only genuine boot entry; not a default compatibility test.
internal static class ResourceBoot540ChildMain
{
    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != 3 || arguments[0] != ChildSwitch)
            return 2;
        string? evidence = null;
        Exception? failure = null;
        ProcessRuntime? captured = null;
        bool driverReturned = false, bootCompleted = false, inputsVerifiedAfter = false, providerVerifiedAfter = false;
        try
        {
            Require(Path.IsPathFullyQualified(arguments[1]) && Digest(arguments[1]) == arguments[2]);
            using var plan = JsonDocument.Parse(File.ReadAllBytes(arguments[1]));
            JsonElement p = plan.RootElement;
            evidence = p.GetProperty("EvidenceDirectory").GetString()!;
            Require(Path.IsPathFullyQualified(evidence) && !Directory.Exists(evidence));
            Directory.CreateDirectory(evidence);
            string directory = evidence;
            await using var diagnostics = new ResourceBoot541Diagnostics(directory);
            VerifyInputs(p);
            string official = p.GetProperty("OfficialAdmissionPath").GetString()!;
            Require(Digest(official) == p.GetProperty("OfficialAdmissionSha256").GetString());
            using var admission = JsonDocument.Parse(File.ReadAllBytes(official));
            Require(admission.RootElement.GetProperty("SourceCommit").GetString() == Pin);
            string installed = admission.RootElement.GetProperty(ManagedContractBatch513Names.InstalledRootProperty).GetString()!;
            string manifestPath = admission.RootElement.GetProperty("ExecutionManifestPath").GetString()!;
            Require(Digest(manifestPath) == admission.RootElement.GetProperty("ExecutionManifestSha256").GetString());
            using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
            VerifyProvider(manifest.RootElement, installed);
            string resourceAdmission = p.GetProperty("ResourceAdmissionPath").GetString()!;
            Require(Digest(resourceAdmission) == p.GetProperty("ResourceAdmissionSha256").GetString());
            using var resource = JsonDocument.Parse(File.ReadAllBytes(resourceAdmission));
            Require(resource.RootElement.GetProperty("Job").GetString() == FreshJob);
            string route = p.GetProperty("Route").GetString()!;
            Require(route is Ordinary or Reviewed);
            string driverPath = p.GetProperty("BackendPath").GetString()!;
            Require(Digest(driverPath) == p.GetProperty("BackendSha256").GetString());
            Assembly driverAssembly = Assembly.LoadFrom(driverPath);
            Require(Path.GetFullPath(driverAssembly.Location) == Path.GetFullPath(driverPath));
            MethodInfo method = driverAssembly.GetType(DriverType, true)!.GetMethod(RunMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
            string[] driverArgs = [resourceAdmission, p.GetProperty("ResourceAdmissionSha256").GetString()!, route, PrivateProfile, p.GetProperty("ObserverOutput").GetString()!];
            string[] dependencies = JsonSerializer.Deserialize<string[]>(StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies))!;
            Require(Hash(StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies)) == p.GetProperty("SelectedCatalogSha256").GetString());
            JsonElement stock = p.GetProperty("StockEts");
            var stockImage = new ManagedBatchImage513(Ets, stock.GetProperty("Path").GetString()!, stock.GetProperty("Sha256").GetString()!);
            Require(Digest(stockImage.Path) == stockImage.Sha256);
            Func<bool, Task<ProcessRuntime>> factory = async owned =>
            {
                Require(owned);
                ProcessRuntime runtime = await StandardBootOriginal412Runtime.Create(
                    official,
                    true,
                    true,
                    owned,
                    (owner, registry) => ManagedStockEts520Preparation.Prepare(
                        owner,
                        registry,
                        stockImage,
                        File.ReadAllBytes(stockImage.Path),
                        dependencies
                    )
                );
                captured = runtime;
                diagnostics.Attach(runtime);
                try
                {
                    diagnostics.Mark(ResourceBoot541Names.Eager);
                    await ResourceBoot541EagerImages.Prepare(
                        runtime,
                        p,
                        dependencies,
                        directory
                    );
                    diagnostics.Mark(ResourceBoot541Names.EagerReady);
                    await Owners(runtime, BeforeBoot);
                    var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    ProcessHandle controller = runtime.Spawn(async context =>
                    {
                        try
                        {
                            diagnostics.Mark(ResourceBoot541Names.PrimFile);
                            _ = await runtime.Modules.Call(context, ManagedContractBatch513Names.PrimFileModule, ManagedContractBatch513Names.OnLoadFunction);
                            diagnostics.Mark(ResourceBoot541Names.PrimFileReady);
                            string[] bootArgs = [ManagedContractBatch513Names.RootOption, installed,
                                ManagedContractBatch513Names.BindirOption, Path.Combine(installed, ManagedContractBatch513Names.ErtsBin),
                                ManagedContractBatch513Names.BootOption, Path.Combine(installed, ManagedContractBatch513Names.BootFile),
                                ManagedContractBatch513Names.ModeOption, ManagedContractBatch513Names.Interactive,
                                ManagedContractBatch513Names.NoShellOption, ManagedContractBatch513Names.NoInputOption];
                            using var deadline = new CancellationTokenSource(ManagedContractBatch513Names.BootDeadline);
                            diagnostics.Mark(ResourceBoot541Names.Boot);
                            _ = await runtime.StartOtpBootAsync(bootArgs.Select(value => new BitString(Encoding.UTF8.GetBytes(value))), deadline.Token);
                            diagnostics.Mark(ResourceBoot541Names.BootReady);
                            string[] names = [ManagedContractBatch513Names.InitializationName, ManagedContractBatch513Names.ApplicationControllerName,
                                ManagedContractBatch513Names.CodeServerName, ManagedContractBatch513Names.LoggerName, FileServer];
                            var witnessed = new List<Term>();
                            foreach (string name in names)
                            {
                                Term pid = await runtime.Modules.Call(
                                    context,
                                    StandardBootOriginal412Names.Erts,
                                    StandardBootOriginal412Names.Whereis,
                                    Term.A(name)
                                );
                                Require(pid is Pid);
                                witnessed.Add(Term.Tuple(Term.A(name), pid));
                            }
                            await File.WriteAllBytesAsync(Path.Combine(directory, Witnesses), ExternalTermFormat.Encode(Term.List(witnessed.ToArray())));
                            completed.TrySetResult();
                        }
                        catch (Exception error)
                        {
                            completed.TrySetException(error);
                        }

                        return Nil.Value;
                    });
                    await completed.Task.WaitAsync(ManagedContractBatch513Names.CaptureDeadline);
                    await controller.Completion.WaitAsync(ManagedContractBatch513Names.ControllerJoinDeadline);
                    await Owners(runtime, AfterBoot);
                    bootCompleted = true;

                    return runtime;
                }
                catch (Exception original)
                {
                    try
                    {
                        await runtime.DisposeAsync();
                    }
                    catch (Exception cleanup)
                    {
                        throw new AggregateException(original, cleanup);
                    }
                    throw;
                }
            };
            Exception? observerFailure = null;
            try
            {
                diagnostics.Mark(ResourceBoot541Names.Observer);
                Task run;
                try
                {
                    run = (Task)method.Invoke(null, [driverArgs, factory])!;
                }
                catch (TargetInvocationException error) when (error.InnerException is { } inner)
                {
                    ExceptionDispatchInfo.Capture(inner).Throw();
                    throw;
                }
                await run;
                diagnostics.Mark(ResourceBoot541Names.Completed);
                driverReturned = true;
            }
            catch (Exception error)
            {
                observerFailure = error;
            }
            finally
            {
                // Retain every independent diagnostic failure; never mask the observer.
                if (captured is not null)
                    await Attempt(async () => await Owners(captured, AfterObserver));
                await Attempt(() =>
 {
     VerifyInputs(p);
     inputsVerifiedAfter = true;

     return Task.CompletedTask;
 });
                await Attempt(() =>
 {
     VerifyProvider(manifest.RootElement, installed);
     providerVerifiedAfter = true;

     return Task.CompletedTask;
 });
            }

            async Task Attempt(Func<Task> action)
            {
                try
                {
                    await action();
                }
                catch (Exception error)
                {
                    observerFailure = observerFailure is null ? error : new AggregateException(observerFailure, error);
                }
            }
            if (observerFailure is not null) ExceptionDispatchInfo.Capture(observerFailure).Throw();

            async Task Owners(ProcessRuntime runtime, string file)
            {
                var rows = runtime.Modules.Exports.Select(export => new { Export = export, Owner = runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity) })
                    .Where(item => item.Owner is PreparedBeamImage).ToArray();
                var actual = rows.GroupBy(item => (PreparedBeamImage)item.Owner!, (IEqualityComparer<PreparedBeamImage>)ReferenceEqualityComparer.Instance).Select(group =>
                {
                    var image = group.Key;
                    Require(p.GetProperty("AllowedImages").EnumerateArray().Any(row => row.GetProperty("Module").GetString() == image.Name && row.GetProperty("Sha256").GetString() == image.OriginalBeamSha256));

                    return new { image.Name, image.OriginalBeamSha256, Exports = group.Select(item => new { item.Export.Function, item.Export.Arity }).ToArray() };
                }).OrderBy(row => row.Name, StringComparer.Ordinal).ToArray();
                if (file == BeforeBoot)
                {
                    foreach (var expected in p.GetProperty("InitialImages").EnumerateArray())
                    {
                        string module = expected.GetProperty("Module").GetString()!;
                        string hash = expected.GetProperty("Sha256").GetString()!;
                        Require(actual.Any(row => row.Name == module && row.OriginalBeamSha256 == hash));
                        foreach (var export in runtime.Modules.Exports.Where(export => export.Module == module))
                            Require(runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity) is PreparedBeamImage owner && owner.OriginalBeamSha256 == hash);
                    }
                }
                await File.WriteAllTextAsync(Path.Combine(directory, file), JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (Exception error)
        {
            failure = error;
            if (evidence is not null)
                await File.WriteAllTextAsync(Path.Combine(evidence, Failure), error.ToString());
            else Console.Error.WriteLine(error);
        }
        if (evidence is not null)
            await File.WriteAllTextAsync(
                Path.Combine(evidence, Completion),
                JsonSerializer.Serialize(
                    new
                    {
                        DriverReturned = driverReturned,
                        InputsVerifiedAfter = inputsVerifiedAfter,
                        ProviderVerifiedAfter = providerVerifiedAfter,
                        ActualBootCompleted = bootCompleted,
                        PhysicalCleanupProof = DriverCleanupProof,
                        FullCompatibilityClaimed = false,
                        WholeContractsClosed = 0,
                        Error = failure?.ToString()
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

        return failure is null ? 0 : 1;
    }

    private static void VerifyInputs(JsonElement plan)
    {
        foreach (var input in plan.GetProperty("Inputs").EnumerateArray())
            Require(Digest(input.GetProperty("Path").GetString()!) == input.GetProperty("Sha256").GetString());
    }

    private static void VerifyProvider(JsonElement rows, string installed)
    {
        Require(rows.GetArrayLength() == 6427);
        Require(Directory.EnumerateFiles(installed, "*", SearchOption.AllDirectories).Count() == rows.GetArrayLength());
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.EnumerateArray())
        {
            string relative = row.GetProperty("Path").GetString()!;
            string path = Path.GetFullPath(Path.Combine(installed, relative));
            Require(!Path.IsPathFullyQualified(relative) && path.StartsWith(Path.GetFullPath(installed) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && paths.Add(path));
            Require(Digest(path) == row.GetProperty("Sha256").GetString());
        }
    }

    private static string Digest(string path)
    {
        Require(Path.IsPathFullyQualified(path));

        return Hash(File.ReadAllBytes(path));
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(Invalid);
    }
}

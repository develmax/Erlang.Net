using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

string root = Directory.GetCurrentDirectory();
string dllDirectory = Path.Combine(root, "artifacts/root-part321-composed2/candidate/tests/Erlang.Tests/bin/Debug/net10.0");
AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(dllDirectory, name.Name + ".dll"))
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dllDirectory, name.Name + ".dll")) : null;
Assembly tests = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dllDirectory, "Erlang.Tests.dll"));
const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
Type fixtureType = tests.GetType("Erlang.Tests.DistributionLifecycleHostFixture", true)!;
Type routes = tests.GetType("Erlang.Tests.DistributionGuestLinkPublicRouteHostChecks", true)!;
object? fixture = null;
Exception? primary = null;
var steps = new List<object>();
object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;
object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;
Task Invoke(string name, params object?[] values) => (Task)routes.GetMethod(name, Members)!.Invoke(null, values)!;
async Task<object?> Result(Task task)
{
    await task.WaitAsync(TimeSpan.FromSeconds(5));
    return task.GetType().GetProperty("Result")?.GetValue(task);
}
try
{
    fixture = await Result((Task)fixtureType.GetMethod("Create", Members)!.Invoke(null, null)!);
    await Result(Invoke("TrapExits", fixture));
    object? link = await Result(Invoke("Call", fixture, "link", null));
    object? packet = await Result(Invoke("TakeNow", fixture));
    object? zeroReceive = await Result(Invoke("Receive", fixture));
    steps.Add(new { Phase = "link-and-zero-receive-before-query", Link = link?.ToString(), Packet = packet?.ToString(), ZeroReceiveWasNull = zeroReceive is null });
    Task query = Invoke("Links", fixture);
    object producer = Property(fixture!, "Producer");
    object context = (await Result((Task)Property(producer, "Ready")))!;
    object runtime = Property(fixture!, "Runtime");
    object gate = Field(runtime, "gate");
    int pending = 0;
    var started = System.Diagnostics.Stopwatch.StartNew();
    while (pending == 0 && !query.IsCompleted && started.Elapsed < TimeSpan.FromSeconds(2))
    {
        lock (gate)
            pending = (int)Property(Property(context, "PendingInfoRequests"), "Count");
        await Task.Yield();
    }
    bool suspended = (bool)Field(context, "SuspensionRequested");
    steps.Add(new { Phase = "cross-actor-links-query-before-owner-pump", QueryCompleted = query.IsCompleted, PendingOwnerInfoRequests = pending, Suspended = suspended });
    if (pending != 1 || query.IsCompleted || suspended)
        throw new InvalidOperationException("Expected actual pending owner snapshot while actor waits for commands.");
    object? pumped = await Result(Invoke("Receive", fixture));
    object? links = await Result(query);
    lock (gate)
        pending = (int)Property(Property(context, "PendingInfoRequests"), "Count");
    steps.Add(new { Phase = "explicit-owner-zero-receive-pumps-existing-query", OwnerReceiveWasNull = pumped is null, QueryCompleted = query.IsCompletedSuccessfully, Links = links?.ToString(), PendingOwnerInfoRequests = pending });
    if (pumped is not null || pending != 0 || !query.IsCompletedSuccessfully)
        throw new InvalidOperationException("Owner pump did not settle actual public query.");
}
catch (Exception failure) { primary = failure; }
finally
{
    if (fixture is IAsyncDisposable disposable)
    {
        try { await disposable.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(15)); }
        catch (Exception failure) { steps.Add(new { CleanupFailure = failure.ToString() }); primary ??= failure; }
    }
}
var identities = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name is "Erlang.Tests" or "Erlang.Runtime" or "Erlang.Terms")
    .Select(assembly => new { Name = assembly.GetName().Name, Path = assembly.Location, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))), Mvid = assembly.ManifestModule.ModuleVersionId }).ToArray();
var report = new { Passed = primary is null, Steps = steps, ActualLoadedAssemblies = identities, PrimaryFailure = primary?.ToString(), CandidateModified = false, PrivateFieldsWritten = false, OriginalVmExecuted = false };
await using var output = new FileStream(Path.Combine(root, "artifacts/guest-links321-query-diagnostic1/actual-report.json"), FileMode.CreateNew);
await JsonSerializer.SerializeAsync(output, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(report));
Environment.ExitCode = report.Passed ? 0 : 1;

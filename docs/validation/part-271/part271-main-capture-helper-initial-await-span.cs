using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

if (args[0] == "raw-child")
{
    await Console.OpenStandardOutput().WriteAsync(new byte[] { 0, 128, 255 });
    await Console.OpenStandardError().WriteAsync(new byte[] { 254, 0, 129 });

    return 3;
}

if (args[0] == "hold-child")
{
    await Task.Delay(TimeSpan.FromSeconds(20));

    return 0;
}

string selfAssembly = Assembly.GetExecutingAssembly().Location;
if (args[0] == "spawn-holder")
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add(selfAssembly);
    start.ArgumentList.Add("hold-child");
    using var child = Process.Start(start)!;
    await File.WriteAllTextAsync(args[1], child.Id.ToString());

    return 0;
}

string mainPath = Path.GetFullPath(args[0]);
string typeName = args[1];
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output))
    throw new IOException("Raw control output must be new");
Directory.CreateDirectory(output);
byte[] before = await File.ReadAllBytesAsync(mainPath);
Assembly main = Assembly.LoadFrom(mainPath);
MethodInfo method = main.GetType(typeName, throwOnError: true)!.GetMethod(
    "Run",
    BindingFlags.NonPublic | BindingFlags.Static
)!;

async Task<JsonElement> Invoke(string[] arguments, string prefix)
{
    var task = (Task)method.Invoke(null, new object[] { "dotnet", arguments, prefix })!;
    await task;
    object result = task.GetType().GetProperty("Result")!.GetValue(task)!;

    return JsonSerializer.SerializeToElement(result, result.GetType());
}

JsonElement raw = await Invoke(new[] { selfAssembly, "raw-child" }, Path.Combine(output, "raw"));
if (raw.GetProperty("ExitCode").GetInt32() != 3 || raw.GetProperty("TimedOut").GetBoolean() ||
    !raw.GetProperty("RawComplete").GetBoolean() ||
    !File.ReadAllBytes(Path.Combine(output, "raw.stdout")).SequenceEqual(new byte[] { 0, 128, 255 }) ||
    !File.ReadAllBytes(Path.Combine(output, "raw.stderr")).SequenceEqual(new byte[] { 254, 0, 129 }))
    throw new Exception("Actual Main raw capture mismatch");

string childFile = Path.Combine(output, "child.pid");
var clock = Stopwatch.StartNew();
JsonElement pipes = await Invoke(new[] { selfAssembly, "spawn-holder", childFile }, Path.Combine(output, "pipes"));
clock.Stop();
using var holder = Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(childFile)));
try
{
    holder.Kill(entireProcessTree: true);
    await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
}
finally
{
    if (!holder.HasExited)
        holder.Kill(entireProcessTree: true);
}
if (pipes.GetProperty("ExitCode").GetInt32() != 0 || pipes.GetProperty("TimedOut").GetBoolean() ||
    pipes.GetProperty("RawComplete").GetBoolean() || clock.Elapsed > TimeSpan.FromSeconds(12))
    throw new Exception("Actual Main bounded inherited-pipe capture mismatch");
if (!before.SequenceEqual(await File.ReadAllBytesAsync(mainPath)))
    throw new Exception("Actual Main assembly changed during controls");
Console.WriteLine(JsonSerializer.Serialize(new
{
    Status = "PASS",
    ActualMainAssembly = mainPath,
    ActualMainSha256 = Convert.ToHexString(SHA256.HashData(before)),
    ActualMainMvid = main.ManifestModule.ModuleVersionId,
    ActualMethod = typeName + ".Run",
    ReflectionActualMainMethodInvoked = true,
    Raw = raw,
    InheritedPipes = pipes,
    Milliseconds = clock.ElapsedMilliseconds,
    OwnedChildTerminated = holder.HasExited,
    NativeExecuted = false
}));

return 0;

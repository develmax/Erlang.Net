using NodeIdentityContrasts;
using System.Diagnostics;
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
if (args[0] == "spawn-holder")
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add(typeof(NodeProcess).Assembly.Location);
    start.ArgumentList.Add("hold-child");
    using var child = Process.Start(start)!;
    await File.WriteAllTextAsync(args[1], child.Id.ToString());
    return 0;
}
Directory.CreateDirectory(args[0]);
string assembly = typeof(NodeProcess).Assembly.Location;
var raw = await NodeProcess.Run("dotnet", new[] { assembly, "raw-child" }, Path.Combine(args[0], "raw"));
if (raw.ExitCode != 3 || raw.TimedOut || !raw.RawComplete ||
    !File.ReadAllBytes(Path.Combine(args[0], "raw.stdout")).SequenceEqual(new byte[] { 0, 128, 255 }) ||
    !File.ReadAllBytes(Path.Combine(args[0], "raw.stderr")).SequenceEqual(new byte[] { 254, 0, 129 }))
    throw new Exception("raw capture mismatch");
string childFile = Path.Combine(args[0], "child.pid");
var clock = Stopwatch.StartNew();
var pipes = await NodeProcess.Run("dotnet", new[] { assembly, "spawn-holder", childFile }, Path.Combine(args[0], "pipes"));
clock.Stop();
using var holder = Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(childFile)));
holder.Kill(entireProcessTree: true);
await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
if (pipes.ExitCode != 0 || pipes.TimedOut || pipes.RawComplete || clock.Elapsed > TimeSpan.FromSeconds(12))
    throw new Exception("bounded inherited-pipe capture mismatch");
Console.WriteLine(JsonSerializer.Serialize(new { Status = "PASS", Raw = raw, InheritedPipes = pipes, Milliseconds = clock.ElapsedMilliseconds, OwnedChildTerminated = holder.HasExited }));
return 0;

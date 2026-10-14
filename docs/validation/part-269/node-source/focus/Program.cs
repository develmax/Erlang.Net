using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

if (args.Length > 0 && args[0] == "--raw-failure")
{
    await Console.OpenStandardOutput().WriteAsync(new byte[] { 0, 255, 13, 10 });
    await Console.OpenStandardError().WriteAsync(new byte[] { 128, 0, 10 });
    return 3;
}
if (args.Length > 0 && args[0] == "--pipe-holder")
{
    await Task.Delay(TimeSpan.FromSeconds(30));
    return 0;
}
if (args.Length > 0 && args[0] == "--exit-with-pipe-holder")
{
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    start.ArgumentList.Add("--pipe-holder");
    using var child = Process.Start(start)!;
    await File.WriteAllTextAsync(args[1], child.Id.ToString());
    return 0;
}
try
{
    string output = Path.GetFullPath(args[0]);
    if (Directory.Exists(output)) throw new IOException("Fresh focused output required");
    Directory.CreateDirectory(output);
    var assembly = Assembly.Load("Erlang.NodeIdentityContrasts");
    var process = assembly.GetType("NodeIdentityContrasts.NodeProcess", true)!;
    var method = process.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static)!;
    async Task<object> Run(string mode, string prefix, string? pid = null)
    {
        string[] childArgs = pid is null ? new[] { Assembly.GetExecutingAssembly().Location, mode } : new[] { Assembly.GetExecutingAssembly().Location, mode, pid };
        Task task = (Task)method.Invoke(null, new object[] { Environment.ProcessPath!, childArgs, prefix })!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
    var first = await Run("--raw-failure", Path.Combine(output, "raw-failure"));
    bool Field(object value, string name) => (bool)value.GetType().GetProperty(name)!.GetValue(value)!;
    int Exit(object value) => (int)value.GetType().GetProperty("ExitCode")!.GetValue(value)!;
    if (Exit(first) != 3 || !Field(first,"RawComplete") || Field(first,"TimedOut") ||
        !File.ReadAllBytes(Path.Combine(output,"raw-failure.stdout")).SequenceEqual(new byte[] { 0,255,13,10 }) ||
        !File.ReadAllBytes(Path.Combine(output,"raw-failure.stderr")).SequenceEqual(new byte[] { 128,0,10 }))
        throw new InvalidOperationException("Real child raw bytes/exit changed");
    var watch = Stopwatch.StartNew();
    string childPid = Path.Combine(output,"owned-descendant.pid");
    object second;
    try
    {
        second = await Run("--exit-with-pipe-holder",Path.Combine(output,"pipe-holder"),childPid);
    }
    finally
    {
        if (File.Exists(childPid))
        {
            try
            {
                using var owned = Process.GetProcessById(int.Parse(File.ReadAllText(childPid)));
                if (!owned.HasExited) owned.Kill(entireProcessTree:true);
                await owned.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (ArgumentException) { }
        }
    }
    if (Exit(second) != 0 || Field(second,"RawComplete") || watch.Elapsed > TimeSpan.FromSeconds(20) || File.ReadAllText(Path.Combine(output,"pipe-holder.exit")).Trim() != "0")
        throw new InvalidOperationException("Real inherited-pipe control did not prove bounded incomplete capture");
    var quote = assembly.GetType("NodeIdentityContrasts.NodeErlangString", true)!.GetMethod("Quote", BindingFlags.Static | BindingFlags.NonPublic)!;
    var quoteRows = new List<object>();
    foreach (var example in new[] {
        (Input: "alpha", Expected: "\"alpha\""),
        (Input: "α&🙂", Expected: "\"α&🙂\""),
        (Input: "a\\b", Expected: "\"a\\\\b\""),
        (Input: "a\"b", Expected: "\"a\\\"b\""),
        (Input: "\n\u0085", Expected: "\"\\x{A}\\x{85}\"") })
    {
        string actual = (string)quote.Invoke(null, new object[] { example.Input })!;
        if (actual != example.Expected) throw new InvalidDataException("Erlang quote bytes changed");
        quoteRows.Add(new { example.Input, example.Expected, Actual=actual });
    }
    bool invalidUtf16 = false;
    try { quote.Invoke(null, new object[] { "\uD800" }); }
    catch (TargetInvocationException ex) when (ex.InnerException is System.Text.EncoderFallbackException) { invalidUtf16 = true; }
    if (!invalidUtf16) throw new InvalidDataException("Invalid UTF16 was silently rewritten");
    await File.WriteAllTextAsync(Path.Combine(output,"quotes.json"),JsonSerializer.Serialize(new { Status="PASS", NativeRoundTripExecuted=false, InvalidUtf16Rejected=invalidUtf16, Rows=quoteRows },new JsonSerializerOptions{WriteIndented=true}));
    var exporter = assembly.GetType("NodeIdentityContrasts.NodeExporter", true)!.GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic)!;
    bool invalidArguments = false;
    try { await (Task)exporter.Invoke(null,new object[] {Array.Empty<string>()})!; }
    catch (ArgumentException) { invalidArguments=true; }
    if (!invalidArguments) throw new InvalidOperationException("Argument gate missing");
    await File.WriteAllTextAsync(Path.Combine(output,"focus.json"),JsonSerializer.Serialize(new { Scope="Managed process/writer controls only",NativeExecuted=false,RawBytesExact=true,ExitCode=3,IncompleteInheritedPipe=true,IncompleteExitCode=0,ElapsedMilliseconds=watch.ElapsedMilliseconds,ArgumentGate=true },new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("PASS actual raw byte child + owned inherited-pipe bounded incomplete capture + argument gate; no native execution");
    return 0;
}
catch(Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

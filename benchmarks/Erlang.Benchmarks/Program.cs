using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Erlang.Benchmarks <report.json>");

    return 2;
}
var results = new List<object>();
async Task Measure(
    string name,
    int operations,
    string unit,
    Func<Task> body
)
{
    await body();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    long allocations = GC.GetTotalAllocatedBytes(true);
    var sw = Stopwatch.StartNew();
    await body();
    sw.Stop();
    results.Add(new
    {
        Name = name,
        WorkUnits = operations,
        Unit = unit,
        Milliseconds = sw.Elapsed.TotalMilliseconds,
        AllocatedBytes = GC.GetTotalAllocatedBytes(true) - allocations,
        WorkUnitsPerSecond = operations / sw.Elapsed.TotalSeconds
    });
}
const int count = 10000;
await Measure(
    "spawn-send-complete",
    count,
    "process lifecycle",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var processes = Enumerable.Range(0, count).Select(_ => r.Spawn(async c =>
     {
         await c.ReceiveAsync(t => t);

         return Term.A("ok");
     })).ToArray();
     foreach (var p in processes)
         r.Send(p.Pid, Term.A("go"));
     var reasons = await Task.WhenAll(processes.Select(p => p.Completion));
     if (reasons.Any(reason => !reason.Equals(Term.A("normal"))))
         throw new InvalidOperationException("Benchmark process failed");
 }
);
await Measure(
    "mailbox-load-and-selective-late-match",
    count,
    "queued message (one late selective receive per workload)",
    async () =>
 {
     var m = new Mailbox();
     for (int i = 0; i < count; i++)
         m.Send(Term.I(i));
     m.Send(Term.A("match"));
     if (await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero) is null)
         throw new InvalidOperationException("Benchmark receive failed");
 }
);
var term = Term.Tuple(Term.A("payload"), Term.List(Enumerable.Range(0, 100).Select(i => (Term)Term.I(i)).ToArray()));
await Measure(
    "etf-roundtrip",
    count,
    "encoded/decoded term with exact equality check",
    () =>
 {
     for (int i = 0; i < count; i++)
         if (!ExternalTermFormat.Decode(ExternalTermFormat.Encode(term)).Equals(term))
             throw new InvalidOperationException("Benchmark ETF roundtrip failed");

     return Task.CompletedTask;
 }
);
var report = new { Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Processors = Environment.ProcessorCount, Configuration = "Run with -c Release; exploratory single-machine baseline, no BEAM comparison or latency percentiles", Results = results };
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
return 0;

using Erlang;
using System.Text.Json;
using System.Security.Cryptography;
await using var runtime = new ProcessRuntime();
var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
var stop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var actor = runtime.Spawn(async context => { ready.SetResult(context); await stop.Task; return Nil.Value; });
try
{
    var owner = await ready.Task;
    using var queue = new HostDistribution(runtime, 8, 2, 2);
    var handle = queue.Attach("allocation-probe-owned", owner);
    var buffer = new HostDistributionBuffer([new byte[8].AsMemory()]);
    for (int n = 0; n < 4096; n++) { if (!queue.TryEnqueueFinalizedHostBuffer(handle, buffer)) throw new InvalidOperationException(); _ = queue.TakeFinalizedHostBuffer(handle, owner, out _); }
    long start = GC.GetAllocatedBytesForCurrentThread();
    for (int n = 0; n < 16384; n++) { if (!queue.TryEnqueueFinalizedHostBuffer(handle, buffer)) throw new InvalidOperationException(); _ = queue.TakeFinalizedHostBuffer(handle, owner, out _); }
    long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
    var result = new { Iterations = 16384, AllocatedBytes = bytes, Snapshot = queue.Snapshot(handle), RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))), NativeComparison = false };
    File.WriteAllText(args[0], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(result));
}
finally { stop.TrySetResult(); await actor.Completion; }

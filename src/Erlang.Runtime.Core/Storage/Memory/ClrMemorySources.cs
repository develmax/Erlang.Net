using System.Numerics;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class ClrMemorySources
{
    private static readonly object gate = new();
    private static readonly ConditionalWeakTable<ProcessRuntime, MemoryRuntimeMarker> runtimes = new();

    internal static void Register(ProcessRuntime runtime)
    {
        lock (gate)
            runtimes.Add(runtime, new());
    }

    internal static async ValueTask<ClrMemorySnapshot> Capture(CancellationToken cancellation)
    {
        ProcessRuntime[] sources;
        lock (gate)
            sources = runtimes.Select(entry => entry.Key).ToArray();
        BigInteger processes = 0, used = 0, native = 0;
        foreach (ProcessRuntime runtime in sources)
        {
            var payload = await runtime.CaptureMemoryPayloadAsync(cancellation);
            processes += payload.Processes;
            used += payload.ProcessesUsed;
            native += payload.Native;
        }
        cancellation.ThrowIfCancellationRequested();
        var storage = ObservedPayloadStorage.Capture(cancellation);

        return new(
            processes,
            used,
            storage.Atom,
            storage.Binary,
            storage.Code,
            native
        );
    }
}

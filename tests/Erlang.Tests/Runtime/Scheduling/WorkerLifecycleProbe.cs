using System.Collections.Concurrent;

namespace Erlang.Tests;

internal sealed class WorkerLifecycleProbe
{
    private readonly ConcurrentDictionary<(string Kind, int Identity), Thread> threads = new();

    internal int Count => threads.Count;

    internal void Entered(string kind, int identity)
    {
        WorkerLifecycleAccess.Require(!Thread.CurrentThread.IsThreadPoolThread);
        WorkerLifecycleAccess.Require(threads.TryAdd((kind, identity), Thread.CurrentThread));
    }

    internal void AssertPhysicallyExited()
    {
        foreach (Thread thread in threads.Values)
            WorkerLifecycleAccess.Require(!thread.IsAlive && thread.Join(0));
    }
}

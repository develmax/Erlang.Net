using System.Diagnostics;

namespace Erlang;

public sealed class Mailbox
{
    private readonly object gate = new();
    private readonly LinkedList<Term> messages = new();
    private TaskCompletionSource changed = NewSignal();
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Count { get { lock (gate) return messages.Count; } }
    public void Send(Term message)
    {
        lock (gate) { messages.AddLast(message); var previous = changed; changed = NewSignal(); previous.TrySetResult(); }
    }
    public int Remove(Func<Term, bool> predicate)
    {
        lock (gate) { int count = 0; for (var n = messages.First; n != null;) { var next = n.Next; if (predicate(n.Value)) { messages.Remove(n); count++; } n = next; } return count; }
    }
    public async ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout, CancellationToken cancellation = default) where T : class
    {
        if (timeout < TimeSpan.Zero) throw new ErlangException("badarg");
        long start = Stopwatch.GetTimestamp();
        while (true)
        {
            cancellation.ThrowIfCancellationRequested(); Task signal;
            lock (gate)
            {
                for (var node = messages.First; node != null; node = node.Next)
                {
                    var match = select(node.Value); if (match is not null) { messages.Remove(node); return match; }
                }
                signal = changed.Task;
            }
            TimeSpan? remaining = timeout - Stopwatch.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero) return null;
            try { if (remaining is { } finite) await signal.WaitAsync(finite, cancellation); else await signal.WaitAsync(cancellation); }
            catch (TimeoutException) { return null; }
        }
    }
}

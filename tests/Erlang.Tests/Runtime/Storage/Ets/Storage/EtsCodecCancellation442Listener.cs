using System.Diagnostics.Tracing;

namespace Erlang.Tests;

internal sealed class EtsCodecCancellation442Listener : EventListener
{
    private readonly CancellationTokenSource cancellation;
    private readonly int thread;
    private readonly int expected;
    internal long Completed { get; private set; }
    internal long Total { get; private set; }
    internal int Cancellations { get; private set; }

    internal EtsCodecCancellation442Listener(CancellationTokenSource cancellation, int expected)
    {
        this.cancellation = cancellation;
        this.expected = expected;
        thread = Environment.CurrentManagedThreadId;
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == EtsCodecCancellation442Names.Source)
            EnableEvents(source, EventLevel.Verbose);
    }

    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (Environment.CurrentManagedThreadId != thread || data.EventId != expected
            || data.Payload is not { Count: 2 } values || values[0] is not long completed
            || values[1] is not long total || completed <= 0 || completed >= total || Cancellations != 0)
            return;

        Completed = completed;
        Total = total;
        Cancellations++;
        cancellation.Cancel();
    }
}

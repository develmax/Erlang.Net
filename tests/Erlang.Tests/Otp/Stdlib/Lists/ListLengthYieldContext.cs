using System.Threading.Channels;

namespace Erlang.Tests;

// Deterministic HOST observation of the production Task.Yield boundary. It
// queues the real continuation; it does not replace the length implementation.
internal sealed class ListLengthYieldContext : SynchronizationContext
{
    private readonly Channel<(SendOrPostCallback Callback, object? State)> callbacks = Channel.CreateUnbounded<(SendOrPostCallback, object?)>();

    public override void Post(SendOrPostCallback callback, object? state)
        => callbacks.Writer.TryWrite((callback, state));

    internal async Task ResumeUntil(Task completion)
    {
        using var deadline = new CancellationTokenSource(ListLengthHostNames.Deadline);
        while (!completion.IsCompleted)
        {
            var available = callbacks.Reader.WaitToReadAsync(deadline.Token).AsTask();
            if (await Task.WhenAny(completion, available) == completion)
                break;
            if (!await available)
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            while (callbacks.Reader.TryRead(out var continuation))
            {
                var previous = Current;
                SetSynchronizationContext(this);
                try
                {
                    continuation.Callback(continuation.State);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            }
        }
        // Do not leave an outstanding channel wait after the owned operation.
        deadline.Cancel();
    }
}

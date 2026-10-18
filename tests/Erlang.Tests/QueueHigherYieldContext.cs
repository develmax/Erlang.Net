using System.Threading.Channels;

namespace Erlang.Tests;

internal sealed class QueueHigherYieldContext : SynchronizationContext
{
    private readonly Channel<(SendOrPostCallback Callback, object? State)> callbacks =
        Channel.CreateUnbounded<(SendOrPostCallback, object?)>();

    public override void Post(SendOrPostCallback callback, object? state)
        => callbacks.Writer.TryWrite((callback, state));

    internal Task<bool> ResumeUntilPhase(Task completion, Func<bool> phase)
        => Pump(completion, phase);

    internal async Task ResumeUntil(Task completion)
    {
        await Pump(completion, () => false);
    }

    private async Task<bool> Pump(Task completion, Func<bool> phase)
    {
        using var deadline = new CancellationTokenSource(QueueHigherCooperativeHostNames.Deadline);
        try
        {
            while (!completion.IsCompleted)
            {
                if (phase() && callbacks.Reader.TryPeek(out _))
                    return true;
                Task<bool> available = callbacks.Reader.WaitToReadAsync(deadline.Token).AsTask();
                if (await Task.WhenAny(completion, available) == completion)
                    break;
                if (!await available)
                    throw new InvalidOperationException(QueueHigherCooperativeHostNames.Failure);
                if (phase())
                    return true;
                if (callbacks.Reader.TryRead(out var continuation))
                {
                    SynchronizationContext? previous = Current;
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

            return false;
        }
        finally
        {
            deadline.Cancel();
        }
    }
}

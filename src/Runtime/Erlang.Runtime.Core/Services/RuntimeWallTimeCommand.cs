namespace Erlang;

internal abstract class RuntimeWallTimeCommand(Pid caller)
{
    internal Pid Caller { get; } = caller;
    internal Task? Acknowledgments { get; private set; }

    internal void RecordAcknowledgments(Task completion)
    {
        if (Acknowledgments is not null) throw new InvalidOperationException(nameof(RuntimeWallTimeCommand));
        Acknowledgments = completion;
    }

    internal TaskCompletionSource Settled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal virtual void Fail(Exception error) => Settled.TrySetException(error);

    internal virtual void Abandon(Exception error) => Settled.TrySetResult();
}

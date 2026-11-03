namespace Erlang;

// A prepared temporary request only. Permanent accounting records never retain it.
internal sealed class NativeResourceMicrostateWork989
{
    private readonly RuntimeMicrostateRequest request;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int state = NativeResource524Names.PreparedMicrostate989;
    internal NativeResourceDomain524 Owner { get; }
    internal LinkedListNode<NativeResourceMicrostateWork989> Node { get; }
    internal Task Completion => completion.Task;
    internal bool IsPrepared => Volatile.Read(ref state) == NativeResource524Names.PreparedMicrostate989;

    internal NativeResourceMicrostateWork989(NativeResourceDomain524 owner, RuntimeMicrostateRequest request)
    {
        Owner = owner;
        this.request = request;
        Node = new(this);
    }

    internal void Publish() => Owner.PublishMicrostate989(this);

    internal bool TryMarkPublished() => Interlocked.CompareExchange(
        ref state,
        NativeResource524Names.QueuedMicrostate989,
        NativeResource524Names.PreparedMicrostate989
    ) == NativeResource524Names.PreparedMicrostate989;

    internal void AbortPrepared(Exception failure)
    {
        if (Interlocked.CompareExchange(
            ref state,
            NativeResource524Names.CompletedMicrostate989,
            NativeResource524Names.PreparedMicrostate989
        ) == NativeResource524Names.PreparedMicrostate989)
            completion.TrySetException(failure);
    }

    internal void Execute()
    {
        Owner.RequireMicrostateWorker989();
        if (Interlocked.CompareExchange(
            ref state,
            NativeResource524Names.RunningMicrostate989,
            NativeResource524Names.QueuedMicrostate989
        ) != NativeResource524Names.QueuedMicrostate989)
            throw new InvalidOperationException(RuntimeMicrostateNames.ThreadMismatch);
        try
        {
            request.Auxiliary989();
            completion.TrySetResult();
        }
        catch (Exception failure)
        {
            try
            {
                request.Failure(failure);
            }
            finally
            {
                completion.TrySetException(failure);
            }
        }
        finally
        {
            Volatile.Write(ref state, NativeResource524Names.CompletedMicrostate989);
        }
    }

    internal void FailQueued(Exception failure)
    {
        if (Interlocked.CompareExchange(
            ref state,
            NativeResource524Names.CompletedMicrostate989,
            NativeResource524Names.QueuedMicrostate989
        ) != NativeResource524Names.QueuedMicrostate989)
            return;
        try
        {
            request.Failure(failure);
        }
        finally
        {
            completion.TrySetException(failure);
        }
    }
}

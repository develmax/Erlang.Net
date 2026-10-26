namespace Erlang;

internal sealed class RuntimeSchedulerAuxiliaryBatch420
{
    internal RuntimeActorDispatcher Owner { get; }
    internal RuntimeSchedulerAuxiliary420[] Work { get; }
    internal Task Completion { get; }
    internal RuntimeSchedulerAuxiliaryState420 State;

    internal RuntimeSchedulerAuxiliaryBatch420(RuntimeActorDispatcher owner, int capacity, Action<int> body)
    {
        Owner = owner;
        Work = Enumerable.Range(0, capacity).Select(_ => new RuntimeSchedulerAuxiliary420(body)).ToArray();
        Completion = Task.WhenAll(Work.Select(work => work.Completion));
    }

    internal void Publish() => Owner.PublishAuxiliary420(this);

    internal void Abort(Exception error) => Owner.AbortAuxiliary420(this, error);
}

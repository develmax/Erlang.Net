namespace Erlang;

internal sealed class DistributionOutputTicket(DistributionOutputSequencer owner, object producer)
{
    internal DistributionOutputSequencer Owner { get; } = owner;
    internal object Producer { get; } = producer;
    internal TaskCompletionSource Turn { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal LinkedListNode<DistributionOutputTicket>? Node { get; set; }
    internal bool Released { get; set; }
    internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Started;
}

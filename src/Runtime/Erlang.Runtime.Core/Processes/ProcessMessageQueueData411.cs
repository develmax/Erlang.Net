namespace Erlang;

internal static class ProcessMessageQueueData411
{
    // Fixed current VM startup policy; +hmqd/custom startup modes are not implemented.
    internal const ProcessMessageQueueDataMode411 Default = ProcessMessageQueueDataMode411.OnHeap;

    internal static ProcessMessageQueueDataMode411 Parse(Term value) => value switch
    {
        Atom { Name: ProcessMessageQueueDataNames411.OnHeap } => ProcessMessageQueueDataMode411.OnHeap,
        Atom { Name: ProcessMessageQueueDataNames411.OffHeap } => ProcessMessageQueueDataMode411.OffHeap,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    internal static Atom Atom(ProcessMessageQueueDataMode411 mode) => Term.A(mode switch
    {
        ProcessMessageQueueDataMode411.OnHeap => ProcessMessageQueueDataNames411.OnHeap,
        ProcessMessageQueueDataMode411.OffHeap => ProcessMessageQueueDataNames411.OffHeap,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    });

    internal static Term ChangeSelf(ProcessContext context, Term value)
    {
        ProcessMessageQueueDataMode411 mode = Parse(value);
        context.Cancellation.ThrowIfCancellationRequested();

        return Atom(context.Mailbox.ExchangeMessageQueueData411(mode));
    }
}

namespace Erlang;

internal sealed class SourcePurgeFenceSession
{
    internal ModuleRegistry Registry
    {
        get;
    }
    internal string Module
    {
        get;
    }
    internal ModuleCodeImage Image
    {
        get;
    }
    internal ModuleSourceMutationTicket Ticket
    {
        get;
    }
    internal long Generation
    {
        get;
    }
    internal SourcePurgeOnLoad432? OnLoad432 { get; set; }

    internal TaskCompletionSource Pending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Settled
    {
        get; private set;
    }
    internal bool Completed
    {
        get; private set;
    }

    internal SourcePurgeFenceSession(
        ModuleRegistry registry,
        string module,
        ModuleCodeImage image,
        ModuleSourceMutationTicket ticket
    )
    {
        Registry = registry;
        Module = module;
        Image = image;
        Ticket = ticket;
        Generation = ticket.Generation;
    }

    internal void Settle(bool complete)
    {
        Settled = true;
        Completed = complete;
    }

    internal void Resume()
    {
        Pending.TrySetResult();
        Completion.TrySetResult(Completed);
    }
}

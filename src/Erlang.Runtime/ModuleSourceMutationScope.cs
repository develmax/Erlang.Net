namespace Erlang;

internal sealed class ModuleSourceMutationScope : IDisposable
{
    [ThreadStatic]
    private static ModuleSourceMutationScope? current;
    private readonly ModuleSourceMutationScope? previous;
    private readonly ModuleSourceMutationTicket ticket;
    private readonly bool ownsTicket;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private bool disposed;

    internal ModuleSourceMutationScope(ModuleSourceMutationTicket ticket, bool ownsTicket)
    {
        ticket.RequireMutation();
        this.ticket = ticket;
        this.ownsTicket = ownsTicket;
        previous = current;
        current = this;
    }

    internal static ModuleSourceMutationTicket? Current(ModuleSourceMutationPermission permission)
    {
        for (var scope = current; scope is not null; scope = scope.previous)
            if (ReferenceEquals(scope.ticket.Permission, permission))
            {
                scope.ticket.RequireMutation();

                return scope.ticket;
            }

        return null;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        if (thread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, this))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidTicket);
        current = previous;
        disposed = true;
        if (ownsTicket)
            ticket.Dispose();
    }
}

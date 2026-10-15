namespace Erlang;

internal sealed class ModuleSourceMutationTicket : IDisposable
{
    private readonly object gate = new();
    private bool sessionOwned;
    private bool released;

    internal ModuleSourceMutationPermission Permission
    {
        get;
    }
    internal long Generation
    {
        get;
    }

    internal ModuleSourceMutationTicket(ModuleSourceMutationPermission permission, long generation)
    {
        Permission = permission;
        Generation = generation;
    }

    internal void RequireMutation()
    {
        lock (gate)
            if (released || sessionOwned || !Permission.IsCurrent(Generation))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidTicket);
    }

    internal void TransferToSession()
    {
        lock (gate)
        {
            if (released || sessionOwned || !Permission.IsCurrent(Generation))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidTicket);
            sessionOwned = true;
        }
    }

    internal void RequireSession(long generation)
    {
        lock (gate)
            if (released || !sessionOwned || Generation != generation || !Permission.IsCurrent(generation))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidSession);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (released)
                return;
            released = true;
        }
        Permission.Release();
    }
}

namespace Erlang;

internal sealed partial class ModuleRegistryApplyFrame
{
    [ThreadStatic]
    private static ModuleRegistryApplyFrame? current;
    private readonly ModuleRegistry registry;
    private readonly ModuleRegistryApplyFrame? previous;
    private readonly ModuleRegistryApplyFrame root;
    private readonly bool ownsWriter;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private ModuleSourceMutationTicket? adopted;
    private bool exited;

    internal ModuleRegistryApplyFrame(ModuleRegistry registry, bool ownsWriter)
    {
        this.registry = registry;
        this.ownsWriter = ownsWriter;
        previous = current;
        root = Find(registry)?.root ?? this;
        current = this;
    }

    internal static ModuleRegistryApplyFrame? Find(ModuleRegistry registry)
    {
        for (var frame = current; frame is not null; frame = frame.previous)
            if (ReferenceEquals(frame.registry, registry))
                return frame.root;

        return null;
    }

    internal ModuleSourceMutationTicket Adopt(ModuleSourceMutationPermission permission)
    {
        if (!ReferenceEquals(root, this) || !ownsWriter || exited || thread != Environment.CurrentManagedThreadId
            || !ReferenceEquals(permission.Registry, registry))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        if (adopted is null)
            adopted = permission.TryAcquire();
        adopted.RequireMutation();

        return adopted;
    }

    internal bool HasAdoptedTicket => root.adopted is not null;

    internal void Exit()
    {
        if (exited || thread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, this))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidTicket);
        current = previous;
        exited = true;
    }

    internal void ReleaseAfterWriter()
    {
        if (ReferenceEquals(root, this))
        {
            ReleaseSourceRoot819();
        }
    }
}

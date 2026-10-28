namespace Erlang;

internal sealed partial class ProcessTermConstruction514
{
    private List<ResourceBinaryDescriptor577>? createdResourceBits577;

    internal void PrepareResourceBitsTracking577()
    {
        ProcessTermConstruction514 owner = magicOwner576 ?? this;
        (owner.createdResourceBits577 ??= new()).EnsureCapacity(checked((owner.createdResourceBits577?.Count ?? 0) + 1));
    }

    internal void TrackResourceBits577(ResourceBinaryDescriptor577 descriptor)
        => (magicOwner576 ?? this).createdResourceBits577!.Add(descriptor);

    internal void RollbackResourceBits577()
    {
        if (magicOwner576 is not null || createdResourceBits577 is not { Count: > 0 } created)
            return;
        created[0].Owner.RollbackResourceBinaryBlocks577(created);
        created.Clear();
    }
}

namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    private DdllLinkedVisibility438? linkedVisibility438;
    private ulong nextLinkedVisibility438;

    // All callers hold the actual runtime gate and recheck in a loop after await.
    // Ending one independent mutation never bypasses the other pending scope.
    private Task? PendingMutation438 => mutation?.Task ?? linkedVisibility438?.Completion.Task;

    internal ErlDriverLinkedScope438 BeginLinkedInitialization438(ulong ownerImage)
    {
        lock (gate)
        {
            DdllParentRow422 owner = RequireLinkedOwner438(ownerImage);
            if (linkedVisibility438 is not null)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            ulong token = checked(nextLinkedVisibility438 + 1);
            var scope = new ErlDriverLinkedScope438(transport.Session438, ownerImage, token);
            var visibility = new DdllLinkedVisibility438(owner, scope);
            // Do not await mutation: this request can come from the genuine
            // dynamic initializer for which that exact DDLL mutation is waiting.
            // Existing owned cleanup may also enter after guest admission closes.
            linkedVisibility438 = visibility;
            nextLinkedVisibility438 = token;

            return scope;
        }
    }

    internal void EndLinkedInitialization438(ErlDriverLinkedScope438 scope, bool cleanupSucceeded)
    {
        lock (gate)
        {
            RequireLinkedScope438(scope, requireRoot: true);
            DdllLinkedVisibility438 held = linkedVisibility438!;
            if (!cleanupSucceeded)
            {
                // Native owner cleanup is unknown. Do not publish partial rows
                // or synthesize unloaded success. Physical settlement owns erase.
                closing = true;
                held.Completion.TrySetException(new InvalidDataException(DriverParentWireNames422.Invalid));
            }
            else
            {
                linkedVisibility438 = null;
                held.Completion.TrySetResult();
            }
            wake.Release();
        }
    }

    private DdllParentRow422 RequireLinkedOwner438(ulong identity)
    {
        if (identity == 0 || !identities.TryGetValue(identity, out var owner)
            || owner.Linked || owner.Identity != identity)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return owner;
    }

    private void RequireLinkedScope438(ErlDriverLinkedScope438 scope, bool requireRoot)
    {
        if (scope is null || !StringComparer.Ordinal.Equals(scope.Session, transport.Session438)) throw new InvalidDataException(DriverParentWireNames422.Invalid);
        DdllParentRow422 caller = RequireLinkedOwner438(scope.OwnerImage);
        if (scope.Token == 0)
        {
            if (requireRoot || linkedVisibility438 is not null)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);

            return;
        }
        if (linkedVisibility438 is not { } root || root.Scope.Token != scope.Token
            || root.Scope.OwnerImage != scope.OwnerImage || !ReferenceEquals(root.Owner, caller)
            || root.Completion.Task.IsCompleted)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        // The target of remove_driver_entry may belong to another real image.
        // Authority is this current root caller/session, not target.OwnerImage.
    }

    private void FaultLinkedVisibility438(Exception failure)
    {
        DdllLinkedVisibility438? held = linkedVisibility438;
        linkedVisibility438 = null;
        held?.Completion.TrySetException(failure);
    }
}

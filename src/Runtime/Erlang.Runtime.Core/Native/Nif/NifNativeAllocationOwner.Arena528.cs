namespace Erlang;

internal sealed unsafe partial class NifNativeAllocationOwner
{
    private bool imageAttached528, imageClosed528;
    internal NifNativeAllocationDomain? Arena528
    {
        get
        {
            lock (gate) return domain;
        }
    }
    private bool ImageHasClosed528
    {
        get
        {
            lock (gate) return imageClosed528;
        }
    }
    internal bool PhysicalImageClosed528
    {
        get
        {
            lock (gate) return !imageAttached528 || imageClosed528;
        }
    }

    internal void AttachImage528()
    {
        lock (gate)
        {
            domain?.ImageAttached528();
            imageAttached528 = true;
        }
    }

    internal void ImageClosed528()
    {
        lock (gate) imageClosed528 = true;
        TryEndDomainMembership();
    }

    internal static void FreeFromCurrentScope528(nint pointer)
    {
        if (pointer == 0) return;
        NifNativeCallbackScope? scope = NifNativeCallbackScope.TryCurrent528;
        if (scope?.Allocations.Arena528 is null && scope is not null)
        {
            scope.Allocations.Free(pointer);

            return;
        }
        NifNativeAllocationOwner actual = NifNativeAllocationAddresses528.Resolve528(pointer);
        if (scope is not null && !ReferenceEquals(scope.Allocations.Arena528, actual.Arena528))
            throw new InvalidOperationException(NifNativeAllocationArena528Names.Pointer);
        actual.Free(pointer);
        if (actual.ImageHasClosed528) actual.TryEndDomainMembership();
    }

    internal nint ReallocateShared528(nint pointer, nuint size)
    {
        if (pointer == 0 || Arena528 is null) return Reallocate(pointer, size);
        NifNativeAllocationOwner actual = NifNativeAllocationAddresses528.Resolve528(pointer);
        if (!ReferenceEquals(Arena528, actual.Arena528))
            throw new InvalidOperationException(NifNativeAllocationArena528Names.Pointer);

        return actual.Reallocate(pointer, size);
    }
}

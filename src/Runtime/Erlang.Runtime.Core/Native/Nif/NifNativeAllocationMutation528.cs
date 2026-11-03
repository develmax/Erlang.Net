namespace Erlang;

internal sealed class NifNativeAllocationMutation528 : IDisposable
{
    private readonly NifNativeAllocationOwner owner;
    private bool entered;

    internal NifNativeAllocationMutation528(NifNativeAllocationOwner owner, int extra)
    {
        this.owner = owner;
        Monitor.Enter(NifNativeAllocationAddresses528.Gate);
        entered = true;
        try
        {
            NifNativeAllocationAddresses528.Reserve528(extra);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void Add528(nint pointer) => NifNativeAllocationAddresses528.Add528(pointer, owner);

    internal void Remove528(nint pointer) => NifNativeAllocationAddresses528.Remove528(pointer);

    public void Dispose()
    {
        if (!entered) return;
        entered = false;
        Monitor.Exit(NifNativeAllocationAddresses528.Gate);
    }
}

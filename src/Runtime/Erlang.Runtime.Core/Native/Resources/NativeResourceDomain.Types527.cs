namespace Erlang;

internal sealed partial class NativeResourceDomain524
{
    private NativeResourceTypeTable527? resourceTypes527;
    private bool closePrepared527;
    private int pendingLoads527;

    internal void BeginTypeLoad527()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, NativeResource524Names.Closed);
            pendingLoads527 = checked(pendingLoads527 + 1);
        }
    }

    internal void EndTypeLoad527()
    {
        lock (gate)
        {
            if (pendingLoads527 == 0) throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            pendingLoads527--;
            if (closePrepared527 && pendingLoads527 == 0 && live.Count == 0) signal.Release();
        }
    }

    internal NativeResourceTypeTable527 ResourceTypes527
    {
        get
        {
            lock (gate) return resourceTypes527 ??= new(runtime, this);
        }
    }

    internal NativeResourceType524 CreatePendingType527(string name, NifNativeResourceImage524 image)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, NativeResource524Names.Closed);
            var type = new NativeResourceType524(
                name,
                image,
                0,
                0,
                0,
                0
            );
            try
            {
                types.Add(type);
            }
            catch
            {
                type.Dispose();
                throw;
            }

            return type;
        }
    }

    internal void DiscardPendingType527(NativeResourceType524 type)
    {
        lock (gate) types.Remove(type);
        type.Dispose();
    }
}

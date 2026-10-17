namespace Erlang;

internal static unsafe class NifNativeBinaryRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<nint, NifNativeBinaryStorage> NativeOwned = new();
    private static long next;
    internal static int NativeOwnedCount
    {
        get
        {
            lock (Gate) return NativeOwned.Count;
        }
    }

    internal static bool TryAllocate(nuint size, out nint reference, out NifNativeBinaryStorage? storage)
    {
        reference = 0;
        storage = NifNativeBinaryStorage.TryAllocate(size, nativeOwned: true);
        if (storage is null)
            return false;
        try
        {
            long identity = Interlocked.Increment(ref next);
            if (identity <= 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ForeignStorage);
            reference = checked((nint)identity);
            lock (Gate)
                NativeOwned.Add(reference, storage);

            return true;
        }
        catch
        {
            storage.ReleaseNativeOwnership();
            throw;
        }
    }

    internal static NifNativeBinaryStorage Get(NifNativeBinaryDescriptor* descriptor)
    {
        lock (Gate)
        {
            if (!NativeOwned.TryGetValue(descriptor->Reference, out var storage))
                throw new InvalidOperationException(NifNativeBinaryNames.ForeignStorage);
            if (descriptor->Data != storage.Pointer || descriptor->Size != storage.Size)
                throw new InvalidOperationException(NifNativeBinaryNames.DescriptorMismatch);

            return storage;
        }
    }

    internal static void Release(nint reference)
    {
        NifNativeBinaryStorage storage;
        lock (Gate)
        {
            if (!NativeOwned.Remove(reference, out storage!))
                throw new InvalidOperationException(NifNativeBinaryNames.ForeignStorage);
        }
        storage.ReleaseNativeOwnership();
    }

    internal static void Transfer(NifNativeBinaryDescriptor* descriptor, NifNativeCallEnvironment environment)
    {
        var storage = Get(descriptor);
        environment.RetainBinaryStorage(storage);
        Release(descriptor->Reference);
        descriptor->Reference = 0;
    }
}

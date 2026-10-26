using System.Runtime.InteropServices;

namespace Erlang;

// Owns actual unmanaged callback-vector storage and the genuine delegate roots.
// This constructor cannot create a partial or universal unsupported callback table.
internal sealed unsafe class ErlDriverCallbackTable420 : SafeHandle
{
    private readonly Delegate[] roots;
    private readonly SafeHandle[] serviceRoots;
    private readonly bool[] serviceRetained;
    private readonly Exception?[] serviceFailures;
    internal Exception? ReleaseFailure { get; private set; }
    internal IReadOnlyList<Exception?> ServiceReleaseFailures => serviceFailures;

    internal ErlDriverCallbackTable420(
        ErlDriverAbiLayout420 layout,
        IReadOnlyDictionary<string, Delegate> callbacks,
        IReadOnlyList<SafeHandle> services
    ) : base(nint.Zero, true)
    {
        var active = ErlDriverCallbackSlots420.Fields.Where(slot => slot.Signature is not null).ToArray();
        if (callbacks.Count != active.Length)
            throw new InvalidOperationException(ErlDriverAbiNames420.IncompleteCallbacks);
        roots = new Delegate[active.Length];
        serviceRoots = services.ToArray();
        serviceRetained = new bool[serviceRoots.Length];
        serviceFailures = new Exception?[serviceRoots.Length];
        var pointers = new nint[active.Length];
        void* memory = null;
        try
        {
            // Native delegates do not keep their implementing DLL mapped.
            // Retain genuine provider/module owners before exporting pointers.
            for (int index = 0; index < serviceRoots.Length; index++)
                serviceRoots[index].DangerousAddRef(ref serviceRetained[index]);
            for (int index = 0; index < active.Length; index++)
            {
                var slot = active[index];
                if (!callbacks.TryGetValue(slot.Name, out var callback)
                    || callback.GetType() != slot.Signature || callback.GetInvocationList().Length != 1)
                    throw new InvalidOperationException(ErlDriverAbiNames420.IncompleteCallbacks);
                roots[index] = callback;
                pointers[index] = Marshal.GetFunctionPointerForDelegate(callback);
                if (pointers[index] == nint.Zero)
                    throw new InvalidOperationException(ErlDriverAbiNames420.IncompleteCallbacks);
            }
            memory = NativeMemory.AllocZeroed(checked((nuint)layout.CallbackBytes));
            if (memory == null) throw new OutOfMemoryException();
            for (int index = 0; index < active.Length; index++)
                Marshal.WriteIntPtr((nint)memory, layout.CallbackOffset(active[index].Name), pointers[index]);
            SetHandle((nint)memory);
        }
        catch (Exception primary)
        {
            NativeMemory.Free(memory);
            ReleaseServices();
            if (ReleaseFailure is not null)
                throw new AggregateException(new[] { primary }.Concat(serviceFailures.OfType<Exception>()));
            throw;
        }
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMemory.Free((void*)handle);
        ReleaseServices();
        GC.KeepAlive(roots);

        return ReleaseFailure is null;
    }

    private void ReleaseServices()
    {
        for (int index = serviceRoots.Length - 1; index >= 0; index--)
        {
            if (!serviceRetained[index]) continue;
            serviceRetained[index] = false;
            try
            {
                serviceRoots[index].DangerousRelease();
            }
            catch (Exception error)
            {
                serviceFailures[index] = error;
                ReleaseFailure ??= error;
            }
        }
    }
}

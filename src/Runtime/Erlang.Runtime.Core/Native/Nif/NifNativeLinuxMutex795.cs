using System.Runtime.InteropServices;
namespace Erlang;

// One capability per ACTUAL ELF bridge namespace / logical VM. No first-target
// owner inference, native-worker TLS propagation, or fabricated language env.
internal sealed unsafe class NifNativeLinuxMutex795
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CreateCallback(byte* name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VoidCallback(nint pointer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TryCallback(nint pointer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint NameCallback(nint pointer);

    private readonly object gate = new();
    private readonly NifNativeAllocationDomain arena;
    private readonly CreateCallback create;
    private readonly VoidCallback destroy, acquire, release;
    private readonly TryCallback attempt;
    private readonly NameCallback name;
    private NifNativeLinuxMutex795Table* table;
    private NifNativeAllocationOwner? bridgeOwner;
    private int active;
    private bool stopped;

    internal static NifNativeLinuxMutex795? TryBind(NifNativeAllocationDomain arena, nint bridge)
    {
        nint register;
        try
        {
            register = NifNativePlatformLoader.Symbol(bridge, NifNativeLinuxMutex795Names.Register);
        }
        catch (InvalidOperationException absent) when (absent.Message == NifNativeDiagnostics.BridgeContract)
        {
            // Old ABI9/496 bridge remains usable for unrelated ordinary NIFs.
            // It has no mutex exports; no successful feature is fabricated.
            return null;
        }

        return new(arena, register);
    }

    private NifNativeLinuxMutex795(NifNativeAllocationDomain arena, nint register)
    {
        this.arena = arena;
        create = Create;
        destroy = Destroy;
        acquire = Lock;
        release = Unlock;
        attempt = TryLock;
        name = Name;
        table = (NifNativeLinuxMutex795Table*)NativeMemory.Alloc((nuint)sizeof(NifNativeLinuxMutex795Table));
        if (table == null) throw new OutOfMemoryException();
        try
        {
            *table = new()
            {
                Version = NifNativeLinuxMutex795Names.Version,
                Size = checked((uint)sizeof(NifNativeLinuxMutex795Table)),
                Create = Marshal.GetFunctionPointerForDelegate(create),
                Destroy = Marshal.GetFunctionPointerForDelegate(destroy),
                TryLock = Marshal.GetFunctionPointerForDelegate(attempt),
                Lock = Marshal.GetFunctionPointerForDelegate(acquire),
                Unlock = Marshal.GetFunctionPointerForDelegate(release),
                Name = Marshal.GetFunctionPointerForDelegate(name)
            };
            if (table->Size != NifNativeLinuxMutex795Names.Size
                || ((delegate* unmanaged[Cdecl]<NifNativeLinuxMutex795Table*, int>)register)(table) != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        }
        catch
        {
            NativeMemory.Free(table);
            table = null;
            throw;
        }
    }

    private void Enter()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(stopped, NifNativeLinuxMutex795Names.Closed);
            active = checked(active + 1);
        }
    }

    private void Exit()
    {
        lock (gate) active--;
    }

    private NifNativeAllocationOwner? ScopedOwner()
    {
        NifNativeAllocationOwner? owner = NifNativeCallbackScope.TryCurrent528?.Allocations;
        if (owner is not null && !ReferenceEquals(owner.Arena528, arena))
            throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);

        return owner;
    }

    private NifNativeAllocationOwner WorkerOwner()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(stopped, NifNativeLinuxMutex795Names.Closed);
            if (bridgeOwner is not null) return bridgeOwner;
            var candidate = new NifNativeAllocationOwner();
            try
            {
                candidate.JoinDomain(arena);
                candidate.AttachImage528();
                candidate.RegisterWindow434(NifNativeLinuxMutex795Names.Window);
                bridgeOwner = candidate;

                return candidate;
            }
            catch (Exception primary)
            {
                try
                {
                    candidate.ImageClosed528();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }
    }

    private NifNativeMutex795 Resolve(nint pointer)
    {
        NifNativeAllocationOwner? scoped = ScopedOwner();
        NifNativeMutex795 mutex = NifNativeAllocationOwner.ResolveMutex795(pointer, scoped);
        if (!ReferenceEquals(mutex.Owner.Arena528, arena))
            throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);

        return mutex;
    }

    private nint Create(byte* value)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;

            return (ScopedOwner() ?? WorkerOwner()).CreateMutex795(value);
        }
        catch (OutOfMemoryException)
        {
            return 0;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
        finally
        {
            if (entered) Exit();
        }
    }

    private void Destroy(nint pointer)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;
            NifNativeMutex795 mutex = Resolve(pointer);
            NifNativeAllocationOwner.DestroyMutex795(pointer, mutex.Owner);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
        finally
        {
            if (entered) Exit();
        }
    }

    private int TryLock(nint pointer)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;

            return Resolve(pointer).TryLock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMutex795Names.Invalid;
        }
        finally
        {
            if (entered) Exit();
        }
    }

    private void Lock(nint pointer)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;
            Resolve(pointer).Lock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
        finally
        {
            if (entered) Exit();
        }
    }

    private void Unlock(nint pointer)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;
            Resolve(pointer).Unlock();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
        finally
        {
            if (entered) Exit();
        }
    }

    private nint Name(nint pointer)
    {
        bool entered = false;
        try
        {
            Enter();
            entered = true;

            return pointer == 0 ? 0 : Resolve(pointer).Name;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
        finally
        {
            if (entered) Exit();
        }
    }

    // Do not wait under namespace/owner gates or free callbacks in flight.
    // Legitimate NIF workers are fenced by active native body / actual unload
    // join, not a pretend thread-completion/TLS claim.
    internal void PrepareClose795()
    {
        lock (gate)
        {
            if (active != 0) throw new InvalidOperationException(NifNativeLinuxMutex795Names.Active);
            stopped = true;
        }
    }

    // Called ONLY AFTER the owning ELF bridge handle physically closed.
    internal void ImageClosed795()
    {
        lock (gate)
        {
            if (!stopped || active != 0) throw new InvalidOperationException(NifNativeLinuxMutex795Names.Active);
            if (table == null) return;
            try
            {
                bridgeOwner?.ImageClosed528();
            }
            finally
            {
                NativeMemory.Free(table);
                table = null;
            }
            GC.KeepAlive(create);
            GC.KeepAlive(destroy);
            GC.KeepAlive(acquire);
            GC.KeepAlive(release);
            GC.KeepAlive(attempt);
            GC.KeepAlive(name);
        }
    }
}

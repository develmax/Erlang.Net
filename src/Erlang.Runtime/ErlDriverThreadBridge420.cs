using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang;

// Loaded only inside the dedicated host. Full callback table must retain this
// SafeHandle until all images and invocations drain, then explicitly close FLS
// services before allowing FreeLibrary. Delegates alone do not root a native DLL.
internal sealed class ErlDriverThreadBridge420 : SafeHandle
{
    private readonly ErlDriverNativeThreads420 threads;
    private readonly Func<ulong, ErlDriverImage420> image;
    private readonly Action<Exception> fault;
    private readonly ErlDriverThreadBridgeTypes420.Reserve reserve;
    private readonly ErlDriverThreadBridgeTypes420.Publish publish;
    private readonly ErlDriverThreadBridgeTypes420.Abort abort;
    private readonly ErlDriverThreadBridgeTypes420.ExchangeOwner exchange;
    private readonly ErlDriverThreadBridgeTypes420.Callbacks callbacks;
    private readonly ErlDriverThreadBridgeTypes420.Close close;
    private readonly TaskCompletionSource<bool> closeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task closeTask;
    private bool servicesClosed;
    private Exception? closeFailure;
    private Exception? reportingFailure;

    internal ErlDriverThreadBridge420(
        string path,
        string expectedSha256,
        ErlDriverNativeThreads420 threads,
        Func<ulong, ErlDriverImage420> image,
        Action<Exception> fault
    )
        : base(nint.Zero, true)
    {
        this.threads = threads;
        this.image = image;
        this.fault = fault;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            closeTask = CloseCore();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
        using var source = new FileStream(
            Path.GetFullPath(path),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        if (!Convert.ToHexString(SHA256.HashData(source)).Equals(expectedSha256, StringComparison.Ordinal))
            throw new InvalidDataException(ErlDriverThreadBridgeNames420.InvalidBinary);
        nint library = NativeLibrary.Load(Path.GetFullPath(path));
        bool safeToFree = true;
        try
        {
            exchange = Export<ErlDriverThreadBridgeTypes420.ExchangeOwner>(library, ErlDriverThreadBridgeNames420.ExchangeOwner);
            callbacks = Export<ErlDriverThreadBridgeTypes420.Callbacks>(library, ErlDriverThreadBridgeNames420.Callbacks);
            close = Export<ErlDriverThreadBridgeTypes420.Close>(library, ErlDriverThreadBridgeNames420.Close);
            var configure = Export<ErlDriverThreadBridgeTypes420.Configure>(library, ErlDriverThreadBridgeNames420.Configure);
            reserve = Reserve;
            publish = Publish;
            abort = Abort;
            int result = configure(reserve, publish, abort);
            if (result != 0)
            {
                int cleanup = close();
                if (cleanup != 0)
                {
                    // Do not unmap an image which may still own FLS callback addresses.
                    safeToFree = false;
                    throw new AggregateException(new System.ComponentModel.Win32Exception(result), new System.ComponentModel.Win32Exception(cleanup));
                }
                throw new System.ComponentModel.Win32Exception(result);
            }
            SetHandle(library);
        }
        catch
        {
            if (safeToFree) NativeLibrary.Free(library);
            closeRequested.TrySetResult(false);
            throw;
        }
    }

    public override bool IsInvalid => handle == nint.Zero;
    internal Exception? CloseFailure => closeFailure;

    internal nint OptionalExport455(string name)
    {
        ObjectDisposedException.ThrowIf(IsClosed || servicesClosed, this);

        return NativeLibrary.TryGetExport(handle, name, out nint pointer) ? pointer : nint.Zero;
    }

    internal nint ExportPointer(string name)
    {
        ObjectDisposedException.ThrowIf(IsClosed || servicesClosed, this);

        return NativeLibrary.GetExport(handle, name);
    }

    private static T Export<T>(nint library, string name) where T : Delegate
        => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    private void Report(Exception error)
    {
        // A managed exception must never unwind through a native ABI callback.
        // Session owner records this fatal service fault and performs its real
        // process/image shutdown; it is never advertised as successful work.
        Interlocked.CompareExchange(ref closeFailure, error, null);
        try
        {
            fault(error);
        }
        catch (Exception reporting)
        {
            Interlocked.CompareExchange(ref reportingFailure, reporting, null);
        }
    }

    private int Reserve(ulong owner, nint output)
    {
        try
        {
            ulong id = threads.Reserve(image(owner));
            Marshal.WriteInt64(output, unchecked((long)id));

            return 0;
        }
        catch (OutOfMemoryException)
        {
            return ErlDriverThreadBridgeNames420.NoMemory;
        }
        catch (Exception error)
        {
            Report(error);

            return ErlDriverThreadBridgeNames420.InvalidArgument;
        }
    }

    private int Publish(ulong id, nint thread)
    {
        try
        {
            threads.Publish(id, thread);

            return 0;
        }
        catch (OutOfMemoryException)
        {
            return ErlDriverThreadBridgeNames420.NoMemory;
        }
        catch (Exception error)
        {
            Report(error);

            return ErlDriverThreadBridgeNames420.InvalidArgument;
        }
    }

    private void Abort(ulong id)
    {
        try
        {
            threads.AbortUncreated(id);
        }
        catch (Exception error)
        {
            Report(error);
        }
    }

    internal ulong ExchangeOwner(ulong owner)
    {
        ObjectDisposedException.ThrowIf(IsClosed || servicesClosed, this);

        return exchange(owner);
    }

    internal ulong ReadOwner()
    {
        ObjectDisposedException.ThrowIf(IsClosed || servicesClosed, this);
        ulong owner = exchange(0);
        try
        {
            return owner;
        }
        finally
        {
            exchange(owner);
        }
    }

    internal unsafe IReadOnlyDictionary<string, Delegate> ReadCallbacks(ErlDriverAbiLayout420 layout)
    {
        ObjectDisposedException.ThrowIf(IsClosed || servicesClosed, this);
        void* table = NativeMemory.AllocZeroed(checked((nuint)layout.CallbackBytes));
        if (table == null) throw new OutOfMemoryException();
        try
        {
            int result = callbacks((nint)table, checked((nuint)layout.CallbackBytes));
            if (result != 0) throw new System.ComponentModel.Win32Exception(result);
            var values = new Dictionary<string, Delegate>(StringComparer.Ordinal);
            foreach (var slot in ErlDriverCallbackSlots420.Fields)
            {
                if (slot.Name != ErlDriverThreadBridgeNames420.EqualThreads
                    && !ErlDriverThreadBridgeNames420.CallbackPrefixes.Any(prefix => slot.Name.StartsWith(prefix, StringComparison.Ordinal))) continue;
                nint function = Marshal.ReadIntPtr((nint)table, layout.CallbackOffset(slot.Name));
                if (function == nint.Zero || slot.Signature is null) throw new InvalidDataException(ErlDriverAbiNames420.IncompleteCallbacks);
                values.Add(slot.Name, Marshal.GetDelegateForFunctionPointer(function, slot.Signature));
            }
            if (values.Count != ErlDriverThreadBridgeNames420.CallbackCount) throw new InvalidDataException(ErlDriverAbiNames420.IncompleteCallbacks);

            return values;
        }
        finally
        {
            NativeMemory.Free(table);
        }
    }

    internal Task CloseServicesAsync()
    {
        closeRequested.TrySetResult(true);

        return closeTask;
    }

    private async Task CloseCore()
    {
        if (!await closeRequested.Task.ConfigureAwait(false)) return;
        await threads.CloseAsync().ConfigureAwait(false);
        // Caller has already joined image finish/entry work. Clear native FLS
        // destructor pointers even on CLR worker threads that outlive this DLL.
        int result = close();
        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
        servicesClosed = true;
        if (closeFailure is { } error)
        {
            if (reportingFailure is { } reporting) throw new AggregateException(error, reporting);
            throw error;
        }
    }

    protected override bool ReleaseHandle()
    {
        if (!servicesClosed)
        {
            closeFailure ??= new InvalidOperationException(ErlDriverThreadBridgeNames420.NotDrained);

            return false;
        }
        try
        {
            NativeLibrary.Free(handle);
            GC.KeepAlive(reserve);
            GC.KeepAlive(publish);
            GC.KeepAlive(abort);

            return true;
        }
        catch (Exception error)
        {
            closeFailure = error;

            return false;
        }
    }
}

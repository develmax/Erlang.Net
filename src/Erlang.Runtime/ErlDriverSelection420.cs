using System.Runtime.InteropServices;

namespace Erlang;

// A real one-shot Windows wait in the own native host. Its private cancel joins
// the physical OS worker before stop_select may close the borrowed HANDLE.
internal sealed class ErlDriverSelection420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SignalCallback(nint token, int error);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Create(
        nint handle,
        SignalCallback signal,
        nint token,
        out nint selection
    );
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Cancel(nint selection);

    private static readonly SignalCallback SignalEntry = Signal;
    private readonly Action signal;
    private readonly ErlDriverServiceFault420 fault;
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly Cancel cancel;
    private readonly TaskCompletionSource<bool> requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task close;
    private GCHandle root;
    private nint selection;
    private bool retained;

    internal ErlDriverSelection420(
        nint handle,
        ErlDriverThreadBridge420 bridge,
        Action signal,
        ErlDriverServiceFault420 fault
    )
    {
        this.bridge = bridge;
        this.signal = signal;
        this.fault = fault;
        cancel = Marshal.GetDelegateForFunctionPointer<Cancel>(bridge.ExportPointer(ErlDriverHostNativeNames420.SelectCancel));
        var create = Marshal.GetDelegateForFunctionPointer<Create>(bridge.ExportPointer(ErlDriverHostNativeNames420.SelectCreate));
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            close = CloseCore();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
        try
        {
            bridge.DangerousAddRef(ref retained);
            root = GCHandle.Alloc(this);
            int error = create(
                handle,
                SignalEntry,
                GCHandle.ToIntPtr(root),
                out selection
            );
            if (error != 0) throw new System.ComponentModel.Win32Exception(error);
        }
        catch
        {
            if (root.IsAllocated) root.Free();
            if (retained)
            {
                retained = false;
                bridge.DangerousRelease();
            }
            requested.TrySetResult(false);
            throw;
        }
    }

    private static void Signal(nint token, int error)
    {
        var owner = (ErlDriverSelection420)GCHandle.FromIntPtr(token).Target!;
        try
        {
            if (error != 0) throw new System.ComponentModel.Win32Exception(error);
            owner.signal();
        }
        catch (Exception failure)
        {
            owner.fault.Record(failure);
        }
    }

    internal Task CancelAsync()
    {
        requested.TrySetResult(true);

        return close;
    }

    private async Task CloseCore()
    {
        if (!await requested.Task.ConfigureAwait(false)) return;
        int error = cancel(selection);
        if (error != 0)
        {
            // A failed physical join is never treated as safe reclamation.
            // Keep the self-root and native-image lease until this failed child
            // exits, rather than expose a dangling reverse callback address.
            var failure = new System.ComponentModel.Win32Exception(error);
            fault.Record(failure);
            throw failure;
        }
        selection = 0;
        root.Free();
        if (retained)
        {
            retained = false;
            bridge.DangerousRelease();
        }
    }
}

using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverNativeAsyncPool420 : SafeHandle
{
    private readonly object gate = new();
    private readonly Exception undrained = new InvalidOperationException(ErlDriverAsyncNames420.NotDrained);
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly ErlDriverAsyncNativeTypes420.Complete complete;
    private readonly ErlDriverAsyncNativeTypes420.Submit submit;
    private readonly ErlDriverAsyncNativeTypes420.Close close;
    private readonly TaskCompletionSource<bool> request = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource submitsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private readonly ErlDriverServiceFault420 fault;
    private bool bridgeRetained;
    private bool closing;
    private bool closed;
    private int activeSubmits;

    internal ErlDriverNativeAsyncPool420(
        ErlDriverThreadBridge420 bridge,
        uint lanes,
        Action<ulong> completed,
        ErlDriverServiceFault420 fault
    )
        : base(nint.Zero, true)
    {
        this.bridge = bridge;
        this.fault = fault;
        Lanes = lanes;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            retirement = Retire();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
        nint pool = nint.Zero;
        ErlDriverAsyncNativeTypes420.Close? closer = null;
        try
        {
            bridge.DangerousAddRef(ref bridgeRetained);
            nint library = bridge.DangerousGetHandle();
            var create = Export<ErlDriverAsyncNativeTypes420.Create>(library, ErlDriverAsyncNames420.Create);
            submit = Export<ErlDriverAsyncNativeTypes420.Submit>(library, ErlDriverAsyncNames420.Submit);
            closer = close = Export<ErlDriverAsyncNativeTypes420.Close>(library, ErlDriverAsyncNames420.Close);
            complete = identity =>
            {
                try
                {
                    completed(identity);
                }
                catch (Exception error)
                {
                    fault.Record(error);
                }
            };
            int result = create(lanes, complete, out pool);
            if (result != 0) throw new System.ComponentModel.Win32Exception(result);
            SetHandle(pool);
        }
        catch (Exception primary)
        {
            request.TrySetResult(false);
            int cleanup = pool != nint.Zero && closer is not null ? closer(pool) : 0;
            if (cleanup == 0 && bridgeRetained)
            {
                bridgeRetained = false;
                bridge.DangerousRelease();
            }
            if (cleanup != 0)
                throw new AggregateException(primary, new System.ComponentModel.Win32Exception(cleanup));
            throw;
        }
    }

    internal uint Lanes { get; }
    public override bool IsInvalid => handle == nint.Zero;

    private static T Export<T>(nint library, string name) where T : Delegate
        => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    internal void Submit(
        uint lane,
        ulong owner,
        ulong identity,
        nint invoke,
        nint data
    )
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            activeSubmits = checked(activeSubmits + 1);
        }
        try
        {
            int result = submit(
                handle,
                lane,
                owner,
                identity,
                invoke,
                data
            );
            if (result != 0) throw new System.ComponentModel.Win32Exception(result);
        }
        finally
        {
            lock (gate)
            {
                activeSubmits--;
                if (closing && activeSubmits == 0) submitsDrained.TrySetResult();
            }
        }
    }

    internal Task CloseAsync()
    {
        lock (gate)
        {
            closing = true;
            if (activeSubmits == 0) submitsDrained.TrySetResult();
            request.TrySetResult(true);
        }

        return retirement;
    }

    private async Task Retire()
    {
        if (!await request.Task.ConfigureAwait(false)) return;
        await submitsDrained.Task.ConfigureAwait(false);
        int result = close(handle);
        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
        closed = true;
        SetHandle(nint.Zero);
        GC.KeepAlive(complete);
        if (bridgeRetained)
        {
            bridgeRetained = false;
            bridge.DangerousRelease();
        }
    }

    protected override bool ReleaseHandle()
    {
        // Never block a CLR finalizer in arbitrary driver code, or unmap a DLL
        // used by an actual native worker. The owning child treats this as fatal.
        if (!closed) fault.Record(undrained);

        return closed;
    }
}

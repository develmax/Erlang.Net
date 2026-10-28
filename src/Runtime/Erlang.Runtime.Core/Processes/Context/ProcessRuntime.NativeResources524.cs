namespace Erlang;

public sealed partial class ProcessRuntime
{
    private NativeResourceDomain524? nativeResources524;
    internal bool HasNativeResources524 => Volatile.Read(ref nativeResources524) is not null;

    internal NativeResourceDomain524 NativeResources524
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, NativeResource524Names.Closed);

                return nativeResources524 ??= new NativeResourceDomain524(this);
            }
        }
    }

    private Task CloseNativeResources524()
    {
        lock (gate)
            return nativeResources524?.BeginClose524() ?? Task.CompletedTask;
    }

    private Task DrainNativeResources524() => CloseNativeResources524();
}

namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimeNativePrimitiveSnapshot NativePrimitives => Modules.CaptureNativePrimitives();

    internal void RequireNativePrimitiveRegistration()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) || IsHaltRequested, this);
    }
}

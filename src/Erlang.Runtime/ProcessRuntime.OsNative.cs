namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal RuntimeOsEnvironment? OsEnvironment { get; }

    internal RuntimeOsEnvironment RequireOsEnvironment()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

            return OsEnvironment ?? throw new PlatformNotSupportedException(OsNativeNames.UnsupportedPlatform);
        }
    }
}

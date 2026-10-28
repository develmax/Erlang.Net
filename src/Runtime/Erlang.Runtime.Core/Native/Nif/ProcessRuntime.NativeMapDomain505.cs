namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal object NativeMapDomain505()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(hamtAdmissionClosed429 || disposed || IsHaltRequested, this);

            return (hamtIndex429 ??= new(this)).Domain;
        }
    }
}

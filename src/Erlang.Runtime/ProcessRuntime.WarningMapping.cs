namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimeWarningMapping WarningMapping { get; }

    internal Term ReadWarningMapping()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

            return WarningMapNative.Value(WarningMapping);
        }
    }
}

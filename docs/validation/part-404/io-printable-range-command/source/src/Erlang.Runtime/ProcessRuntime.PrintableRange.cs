namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimePrintableRange PrintableRange { get; }

    internal Term ReadPrintableRange()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

            return PrintableRange == RuntimePrintableRange.Unicode
                ? IoPrintableRangeNativeAtoms.Unicode
                : IoPrintableRangeNativeAtoms.Latin1;
        }
    }
}

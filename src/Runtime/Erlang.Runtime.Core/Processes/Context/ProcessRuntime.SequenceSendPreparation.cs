namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void CaptureSequenceSendPreparation(
        ProcessContext sender,
        out SequenceTraceSendPreview preview,
        out ProcessTermBorrow506? borrow
    )
    {
        lock (gate)
        {
            sender.Cancellation.ThrowIfCancellationRequested();
            ThrowIfHalted();
            preview = SequenceTracing.PreviewSent(sender);
            borrow = sender.SequentialTrace.BorrowPhysical();
        }
    }
}

namespace Erlang;

public sealed partial class ProcessContext
{
    internal object? CaptureTraceErrorFrame427() => executionFrame.Value;

    internal bool IsTraceErrorFrame427(object? frame) => ReferenceEquals(executionFrame.Value, frame);
}

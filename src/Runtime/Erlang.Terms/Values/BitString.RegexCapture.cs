namespace Erlang;

public sealed partial class BitString
{
    internal BitString? PinRegexCaptureSource()
    {
        backing.Pin();
        if (!IsBinary
            || LogicalBitOffset577 % BitStringLayout.BitsPerByte != 0
            || BitLength <= BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte)
            return null;

        return this;
    }
}

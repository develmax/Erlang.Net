namespace Erlang;

public sealed partial class BitString
{
    internal BitString? PinRegexCaptureSource()
    {
        backing.Pin();
        if (!IsBinary
            || bitOffset % BitStringLayout.BitsPerByte != 0
            || BitLength <= BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte)
            return null;

        return this;
    }
}

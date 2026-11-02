namespace Erlang;

internal sealed partial class BinaryBacking
{
    // Caller owns the actual native copy-read lease, if any, and releases it
    // after leaving any source heap/domain read scope.
    internal BitString CopyValueBits255(long offset, int bits)
    {
        if (native524 is not null)
            return BitString.CopyValueRange255(native524.Bytes524, offset, bits);
        lock (gate)
        {
            return BitString.CopyValueRange255(buffer, offset, bits);
        }
    }
}

namespace Erlang;

internal sealed unsafe partial class OrdinaryBinaryBacking
{
    // The authenticated live BinRef supplies lifetime. A query must not pin,
    // retain, resize, export or revoke a writable producer.
    internal NativeBinaryInfoRow822 ReadBinaryInfo822(nint expectedBinary)
    {
        lock (gate)
        {
            nint selected = Require();
            if (selected != expectedBinary)
                throw new InvalidOperationException(NativeBinaryInfo822Names.Invalid);
            var header = (OrdinaryBinaryHeader*)selected;

            return new(
                checked((ulong)(nuint)selected),
                checked((ulong)header->OriginalSize),
                header->References
            );
        }
    }
}

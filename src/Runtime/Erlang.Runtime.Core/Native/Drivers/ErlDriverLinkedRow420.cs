namespace Erlang;

internal sealed class ErlDriverLinkedRow420
{
    internal ErlDriverLinkedRow420(ErlDriverImage420 image, ErlDriverEntry420 entry, ErlDriverImageLease420 lease)
    {
        Image = image;
        Entry = entry;
        Lease = lease;
    }

    internal ErlDriverImage420 Image { get; }
    internal ErlDriverEntry420 Entry { get; }
    internal ErlDriverImageLease420 Lease { get; }
    internal object DriverGate { get; } = new();
    internal ulong Identity { get; set; }
}

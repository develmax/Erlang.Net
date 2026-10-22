namespace Erlang;

internal sealed class ErlDriverHostSlot420
{
    internal ErlDriverHostSlot420(
        ulong identity,
        ErlDriverImage420 image,
        ErlDriverEntry420 entry,
        object driverGate
    )
    {
        Identity = identity;
        Image = image;
        Entry = entry;
        DriverGate = driverGate;
    }

    internal ulong Identity { get; }
    internal ErlDriverImage420 Image { get; }
    internal ErlDriverEntry420 Entry { get; }
    internal object DriverGate { get; }
}

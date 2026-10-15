using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Erlang;

internal readonly struct HostDistributionBufferStorage(
    ReadOnlyCollection<BitString> segments,
    long payloadBytes
)
{
    internal ReadOnlyCollection<BitString> Segments { get; } = segments;
    internal long PayloadBytes { get; } = payloadBytes;
    internal long AccountedBytes => checked(
        PayloadBytes + (long)Segments.Count * IntPtr.Size + Unsafe.SizeOf<HostDistributionBufferStorage>()
    );
}

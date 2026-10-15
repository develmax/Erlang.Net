using System.Collections.ObjectModel;

namespace Erlang;

public sealed class HostDistributionBuffer
{
    private readonly HostDistributionBufferStorage storage;

    public HostDistributionBuffer(IEnumerable<ReadOnlyMemory<byte>> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var snapshot = new List<BitString>();
        long size = 0;
        foreach (var segment in segments)
        {
            if (snapshot.Count == HostDistributionParameters.MaximumSegmentsPerBuffer)
                throw new ArgumentException(HostDistributionDiagnostics.BufferSegments, nameof(segments));
            snapshot.Add(new BitString(segment.Span));
            size = checked(size + segment.Length);
        }
        storage = new(new ReadOnlyCollection<BitString>(snapshot.ToArray()), size);
    }

    public IReadOnlyList<BitString> Segments => storage.Segments;
    public long PayloadBytes => storage.PayloadBytes;
    public int SegmentCount => storage.Segments.Count;
    internal long AccountedBytes => storage.AccountedBytes;
}

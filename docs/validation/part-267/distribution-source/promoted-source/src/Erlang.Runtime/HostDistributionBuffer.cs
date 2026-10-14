using System.Collections.ObjectModel;

namespace Erlang;

public sealed class HostDistributionBuffer
{
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
        Segments = new ReadOnlyCollection<BitString>(snapshot);
        PayloadBytes = size;
    }

    public IReadOnlyList<BitString> Segments { get; }
    public long PayloadBytes { get; }
    public int SegmentCount => Segments.Count;
}

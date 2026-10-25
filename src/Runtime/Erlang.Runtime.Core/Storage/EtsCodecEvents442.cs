using System.Diagnostics.Tracing;

namespace Erlang;

[EventSource(Name = EtsCodecEventNames442.Source)]
internal sealed class EtsCodecEvents442 : EventSource
{
    internal static readonly EtsCodecEvents442 Log = new();

    private EtsCodecEvents442() { }

    [Event(EtsCodecEventNames442.EncodeProgress, Level = EventLevel.Verbose)]
    public void EncodeProgress(long completedBytes, long totalBytes)
    {
        if (IsEnabled()) WriteEvent(EtsCodecEventNames442.EncodeProgress, completedBytes, totalBytes);
    }

    [Event(EtsCodecEventNames442.DecodeProgress, Level = EventLevel.Verbose)]
    public void DecodeProgress(long completedBytes, long totalBytes)
    {
        if (IsEnabled()) WriteEvent(EtsCodecEventNames442.DecodeProgress, completedBytes, totalBytes);
    }
}

using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimeTimerEntry628(long milliseconds, RuntimePendingPayload628 payload)
{
    internal long Milliseconds { get; set; } = milliseconds;
    internal long Started { get; set; } = Stopwatch.GetTimestamp();
    internal bool Paused { get; set; }
    internal RuntimePendingPayload628 Payload { get; } = payload;
    internal ReferenceTerm Reference => (ReferenceTerm)Payload.Value.Items[RuntimeTimerPayloadLayout628.Reference];
    internal Term Destination => Payload.Value.Items[RuntimeTimerPayloadLayout628.Destination];
    internal Term Message => Payload.Value.Items[RuntimeTimerPayloadLayout628.Message];
    internal Timer Timer { get; set; } = null!;
    internal bool RetirementStarted { get; set; }
    internal List<Exception>? CallbackFailures { get; set; }
    internal long Remaining => Paused ? Milliseconds : Math.Max(
        0,
        Milliseconds - Stopwatch.GetElapsedTime(Started).Ticks / TimeSpan.TicksPerMillisecond
    );
}

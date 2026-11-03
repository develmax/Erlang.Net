namespace Erlang;

internal sealed partial class ErlDriverAsyncServices420
{
    internal Task RemovePortAsync1200(ErlDriverPort420 port)
    {
        // StopCore has sealed submissions and run the actual stop callback.
        // Keep the parent's closing row until its admitted jobs have either
        // delivered ready_async or freed their data/image/PDL ownership.
        lock (gate)
            return Task.WhenAll(pending.Values
                .Where(job => ReferenceEquals(job.Port, port))
                .Select(job => job.Done.Task));
    }
}

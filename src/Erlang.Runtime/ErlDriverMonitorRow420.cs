namespace Erlang;

internal sealed class ErlDriverMonitorRow420
{
    internal ErlDriverMonitorRow420(ulong attempt, ErlDriverPort420 port, nuint process)
    {
        Attempt = attempt;
        Port = port;
        Process = process;
    }

    internal ulong Attempt { get; }
    internal ErlDriverPort420 Port { get; }
    internal nuint Process { get; }
    internal TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal byte[]? Prefix { get; set; }
    internal bool Removed { get; set; }
    internal bool Exited { get; set; }
}

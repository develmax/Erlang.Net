namespace Erlang.PortProbe;

internal static class PortProbeParameters
{
    public const int BufferBytes = 4096;
    public const string Emit = "emit";
    public const string Lines = "lines";
    public const string LinePayload = "abc\n1234\r\n\nend\r";
    public const string Payload = "port-probe";
    public const int ExitCode = 7;
    public const string SlowEcho = "slow-echo";
    public const string DrainFile = "drain-file";
    public const int StartDelayMilliseconds = 2000;
    public static readonly byte[] ReadyPacket = [0, 0, 0, 1, 1];
}

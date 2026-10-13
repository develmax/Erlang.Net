namespace Erlang.PortProbe;

internal static class PortProbeParameters
{
    public const int BufferBytes = 4096;
    public const string Emit = "emit";
    public const string Lines = "lines";
    public const string LinePayload = "abc\n1234\r\n\nend\r";
    public const string Payload = "port-probe";
    public const int ExitCode = 7;
}

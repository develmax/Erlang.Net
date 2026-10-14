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
    public const string HeldDrainFile = "held-drain-file";
    public const int ReleasePollMilliseconds = 5;
    public const string Streams = "streams";
    public const string ArgumentZero = "argv0";
    public const string InputEof = "input-eof";
    public const string Auxiliary = "auxiliary";
    public const int AuxiliaryInputDescriptor = 3;
    public const int AuxiliaryOutputDescriptor = 4;
    public const string ProcessCommandLine = "/proc/self/cmdline";
    public const string WindowsKernel = "kernel32.dll";
    public const string WindowsShell = "shell32.dll";
    public static readonly byte[] OutputFirst = [1, 11];
    public static readonly byte[] ErrorFirst = [1, 12];
    public static readonly byte[] OutputSecond = [1, 13];
    public static readonly byte[] ErrorSecond = [1, 14];
    public static readonly byte[] InputClosedPacket = [1, 17];
    public const int StartDelayMilliseconds = 2000;
    public static readonly byte[] ReadyPacket = [0, 0, 0, 1, 1];
}

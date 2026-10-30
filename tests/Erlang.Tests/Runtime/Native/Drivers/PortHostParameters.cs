namespace Erlang.Tests;

internal static class PortHostParameters
{
    public const int EchoPayloadBytes = 3;
    public const string StatisticsInput = "input";
    public const string StatisticsOutput = "output";
    public const string Test = "runtime/ports-real-executable-ownership-and-resource-cleanup";
    public const string Probe = "tools/Native/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll";
    public const string RuntimeRoot = "../../..";
    public const string WindowsHost = "dotnet.exe";
    public const string UnixHost = "dotnet";
    public const string Failure = "Port resource contract failed";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}

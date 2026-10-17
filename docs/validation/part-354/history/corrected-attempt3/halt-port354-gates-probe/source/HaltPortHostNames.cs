namespace Erlang.Tests;

internal static class HaltPortHostNames
{
    public const string ProfilingOwner = "SystemProfiling";
    public const string Get = "Get";
    public const string Test = "runtime/halt-live-port-drain-immediate-timeout-and-profile-ownership";
    public const string Profile = "system_profile";
    public const string Timestamp = "timestamp";
    public const string Undefined = "undefined";
    public const string DrainFile = "drain-file";
    public const string HeldDrainFile = "held-drain-file";
    public const string ReleaseSuffix = ".release";
    public const string FileDirectory = "artifacts/halt-port-files";
    public const string Failure = "Halt live-port resource contract failed";
    public const int Status = 41;
    public const int ImmediateStatus = 43;
    public const int TimeoutStatus = 255;
    public const uint TimeoutMilliseconds = 100;
    public const int PayloadBytes = 262144;
    public const int PayloadPattern = 251;
    public const int PacketBytes = 4;
    public const int EchoByte = 37;
    public const int MetadataBytes = 4;
    public static readonly byte[] ExpectedHeader = [0, 4, 0, 0];
    public static readonly byte[] Ready = [1];
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}

namespace Erlang.Tests;

internal static class RuntimeStatisticsHostParameters
{
    public const string Input = "input";
    public const string Output = "output";
    public const string Test = "runtime/statistics-two-owners-real-clock-user-cpu-cursors";
    public const int Owners = 2;
    public const int SamplesPerOwner = 64;
    public const int BusySpinIterations = 256;
    public const int DirtyQueueKinds = 2;
    public static readonly TimeSpan BusyInterval = TimeSpan.FromMilliseconds(2);
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
    public const string Failure = "Statistics actual shared clock/user CPU contract failed";
    public const int MicrostateFields = 3;
}

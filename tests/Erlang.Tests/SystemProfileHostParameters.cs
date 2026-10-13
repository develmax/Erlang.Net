namespace Erlang.Tests;

internal static class SystemProfileHostParameters
{
    public const string Test = "runtime/system-profile-real-shared-state-isolation-death-and-validation";
    public const string Failure = "System profiler control contract failed.";
    public const int Owners = 2;
    public const int Exchanges = 32;
    public const string ForeignNode = "profile@foreign";
    public const string UnknownOption = "unknown_profile_option";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
}

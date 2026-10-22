namespace Erlang.Tests;

internal static class InternalPorts416Names
{
    internal const string AdmissionTest = "runtime/internal-ports416/atomic-registration-and-foreign-actor-refusal";
    internal const string LiveTest = "runtime/internal-ports416/real-open-command-connect-info-close-and-child-exit";
    internal const string Failure = "Native internal port registration or actual owner lifecycle differs";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    internal static readonly byte[] First = [1, 2, 3];
    internal static readonly byte[] Second = [4, 5, 6];
}

namespace Erlang.Tests;

internal static class DistributionPendingTestNames
{
    internal const string Test = "runtime/distribution-pending-owned-resource-generation-kernel-controller-lifecycle";
    internal const string FirstNode = "pending_first@localhost";
    internal const string SecondNode = "pending_second@localhost";
    internal const string ThirdNode = "pending_third@localhost";
    internal const string InvalidNode = "invalid.prefix@localhost";
    internal const string Kill = "kill";
    internal const string Failure = "Runtime pending distribution focus assertion failed";
    internal const int MaximumNodes = 2;
    internal const int Repetitions = 32;
    internal const int ConnectionIdBits = 24;
    internal const uint ConnectionIdMask = (1u << ConnectionIdBits) - 1;
    internal const long Uint32Modulus = 1L << 32;
    internal const long NativeSmallMaximum = (1L << 59) - 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}

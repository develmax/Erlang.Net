using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsLifecycle586Names
{
    internal const string GateTest = "runtime/atomics586/shutdown-waits-outside-runtime-gate";
    internal const string HeapTest = "runtime/atomics586/shutdown-retains-backing-until-owned-heap-retires";
    internal const string Gate = "gate", Owner = "atomics413", Enter = "Enter";
    internal const string Heap = "TermHeap506", Reserve = "Reserve", Set = "Set";
    internal const string Failure = "Atomics shutdown gate or physical root ordering failed";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan Probe = TimeSpan.FromMilliseconds(250);
}

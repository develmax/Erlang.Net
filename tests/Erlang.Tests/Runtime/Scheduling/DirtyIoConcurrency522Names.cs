using System.Reflection;

namespace Erlang.Tests;

internal static class DirtyIoConcurrency522Names
{
    internal const string HeldTest = "dirty-io522/held-job-leaves-real-control-lane";
    internal const string CloseTest = "dirty-io522/close-joins-every-IO-worker";
    internal const int ProfileIoWorkers = 2;
    internal const string PoolSnapshot = "PoolSnapshot522";
    internal const string Workers = "Workers";
    internal const string CpuWorkers = "CpuWorkers";
    internal const string IoWorkers = "IoWorkers";
    internal const string ThreadId = "ThreadId";
    internal const string WorkerKind = "Kind";
    internal const string WorkerAlive = "Alive";
    internal const string WorkerJoined = "Joined";
    internal const string WorkerQuiesced = "Quiesced";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
}

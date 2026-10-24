using System.Reflection;

namespace Erlang.Tests;

internal static class CollectorHibernate415Names
{
    internal const string ChildSwitch = "--collectors415-hibernate-child";
    internal const string Receipt = "hibernate-survival.json";
    internal const string JournalPrefix = "phase-";
    internal const string Json = ".json";
    internal const string Phase = "hibernate-phase.json";
    internal const string FailureFile = "hibernate-failure.json";
    internal const string Failure = "Actual original collector timeout/hibernate/wake protocol failed";
    internal const string InvalidArguments = "Expected hibernate child switch and absolute output path";
    internal const string Gate = "gate";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const string ExecutionIdentity = "ExecutionIdentity";
    internal const string Image = "Image";
    internal const string Done = "Done";
    internal const string Waiting = "WaitingForMessage";
    internal const string System = "IsSystemProcess";
    internal const string Erts = "erts_internal";
    internal const string Spawn = "spawn_system_process";
    internal const string Notify = "notify";
    internal const string CheckGc = "check_gc";
    internal const string NeedGc = "need_gc";
    internal const string Area = "dead_target_area";
    internal const string InitialPhase = "source-started";
    internal const string HibernatePhase = "both-actual-transfers-observed";
    internal const string WakePhase = "both-original-source-reactions-completed";
    internal const string DeadPhase = "dead-target-three-how-acks-verified";
    internal const string DisposedPhase = "actual-dispose-completed";
    internal const int ArgumentsExit = 2, FailureExit = 1, SuccessExit = 0;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan InitialDeadline = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan HibernateDeadline = TimeSpan.FromSeconds(75);
    internal static readonly TimeSpan WakeDeadline = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
}

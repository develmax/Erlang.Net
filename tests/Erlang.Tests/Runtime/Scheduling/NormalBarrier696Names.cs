using System.Reflection;

namespace Erlang.Tests;

internal static class NormalBarrier696Names
{
    internal const string ActualTest = "runtime/normal-barrier-actual-turns-auxiliary-and-dirty";
    internal const string CancellationTest = "runtime/normal-barrier-concurrent-request-cancellation";
    internal const string RetirementTest = "runtime/normal-barrier-reentry-and-retirement";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Acquire = "AcquireNormalBarrier696";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const string Dispatcher = "actorDispatcher";
    internal const string PrepareAuxiliary = "PrepareAuxiliary420";
    internal const string Publish = "Publish";
    internal const string Completion = "Completion";
    internal const string Retire = "CompleteWhenIdle";
    internal const string Barrier = "normalBarrier696";
    internal const string Scheduler = "Scheduler696";
    internal const string Owner = "Owner";
    internal const string Reentrant = "A normal execution barrier cannot queue behind the same requester.";
    internal const string Failure = "The actual normal execution barrier invariant failed.";
    internal const string CallbackFailure = "Expected barrier callback failure.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}

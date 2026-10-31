namespace Erlang.Tests;

internal static class WorkerLifecycleNames
{
    internal const string PartialNormal = "runtime/worker-lifecycle/partial-normal-entry-failure";
    internal const string PartialDirty = "runtime/worker-lifecycle/partial-dirty-entry-failure";
    internal const string Constructor = "runtime/worker-lifecycle/prefix-and-initializer-rollback";
    internal const string ConcurrentClose = "runtime/worker-lifecycle/close-versus-actual-ready";
    internal const string IndependentErrors = "runtime/worker-lifecycle/independent-entry-errors-aggregate";
    internal const string MissingFailure = "Actual worker failure unexpectedly completed successfully";
    internal const string MissingMarker = "Actual worker failure did not preserve expected marker: ";
    internal const string Failure = "Actual worker startup, physical join or owned-resource rollback differs";
    internal const string Policy = "Erlang.RuntimeWorkerCreationPolicy";
    internal const string Dispatcher = "Erlang.RuntimeActorDispatcher";
    internal const string DirtyOwner = "Erlang.DirtyNativeOwner419";
    internal const string Normal = "normal";
    internal const string DirtyCpu = "dirty-cpu";
    internal const string DirtyIo = "dirty-io";
    internal const string PrefixBeforeDispatcher = "before-dispatcher";
    internal const string PrefixBeforeAtoms = "before-atoms";
    internal const string PrefixAfterStreams = "after-streams";
    internal const string BeforeInitializer = "before-initializer";
    internal const string BeforeClrRegistration = "before-clr-registration";
    internal const string NormalFirst = "actual-normal-entry-one";
    internal const string NormalSecond = "actual-normal-entry-two";
    internal const string NormalBeforeStart = "actual-normal-before-start-two";
    internal const string DirtySecond = "actual-dirty-io-entry-two";
    internal const string DirtyBeforeStart = "actual-dirty-io-before-start-two";
    internal const string ConstructorError = "actual-constructor-checkpoint";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(12);
}

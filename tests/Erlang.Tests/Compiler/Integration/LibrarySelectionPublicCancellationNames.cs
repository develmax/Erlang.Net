using System.Reflection;

namespace Erlang.Tests;

internal static class LibrarySelectionPublicCancellationNames
{
    internal const string SetupTest = "runtime/library-selection/public-factory-cancel-during-setup";
    internal const string PermitTest = "runtime/library-selection/public-factory-cancel-after-purger-permit";
    internal const string ObserverField = "libraryStartupObserver";
    internal const string SetupPhase = "setup-before-prepare";
    internal const string PermitPhase = "purge-permit-acquired";
    internal const string CleanupStartedPhase = "cleanup-started";
    internal const string RetirementMarkedPhase = "cleanup-core-retirement-marked";
    internal const string DispatcherCompletion = "ActorDispatchCompletion";
    internal const string Initializer = "initializer";
    internal const string Completion = "Completion";
    internal const string Disposed = "disposed";
    internal const string Failure = "Public library factory cancellation or physical cleanup failed";
    internal const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}

namespace Erlang.Tests;

internal static class IdentifierObservationShutdownHostNames
{
    internal const string DisposeWithMonitorAndLinkTest = "HOST/identifier-observer-dispose-live-monitor-link";
    internal const string InitializerDeathWithMonitorAndLinkTest = "HOST/identifier-observer-init-death-live-monitor-link";
    internal const string HaltWithLiveCallbacksTest = "HOST/identifier-observer-halt-live-callbacks";
    internal const string ClosedPublicationRaceTest = "HOST/identifier-observer-close-resolve-publication-race";
    internal const string UnexpectedOpenResolverErrorTest = "HOST/identifier-observer-unexpected-open-resolver-error";

    internal const string KnownNodesProperty = "KnownNodes";
    internal const string ClosedField = "observedIdentitiesClosed";
    internal const string IncomingTermProperty = "IncomingTerm";
    internal const string KnownNodesType = "Erlang.RuntimeKnownNodes";
    internal const string LocalIdentityProperty = "LocalIdentity";
    internal const string ObserveMethod = "Observe";
    internal const string CloseObservedMethod = "CloseObservedIdentities";
    internal const string GetIdentityMethod = "GetDistributionEntryIdentity";
    internal const string TryGetIdentityMethod = "TryGetDistributionEntryIdentity";
    internal const string ObservedField = "observed";
    internal const string TryGetValueMethod = "TryGetValue";
    internal const string Remote = "observer403_remote@localhost";
    internal const string UnexpectedResolverFailure = "Unexpected open resolver failure must propagate.";
    internal const string Down = "DOWN";
    internal const string Kill = "kill";
    internal const string Failure = "Identifier observer shutdown callback control failed.";
    internal const int HaltStatus = 0;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}

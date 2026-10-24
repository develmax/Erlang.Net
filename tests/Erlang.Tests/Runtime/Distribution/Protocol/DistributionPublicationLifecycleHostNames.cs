namespace Erlang.Tests;

internal static class DistributionPublicationLifecycleHostNames
{
    internal const string Test = "host_distribution_publication_commit_cleanup_and_quiescence";
    internal const string Local = "publication_lifecycle@local";
    internal const string Remote = "publication_lifecycle@remote";
    internal const string SecondRemote = "publication_lifecycle@second";
    internal const string Filler = "publication_lifecycle_filler";
    internal const string Failure = "Distribution publication lifecycle assertion failed";
    internal const string Registered = "publication_lifecycle_registered";
    internal const string InjectedFailure = "Expected distribution lifecycle cancellation callback failure";
    internal const string Undefined = "undefined";
    internal const string ProcessFlag = "process_flag";
    internal const string TrapExit = "trap_exit";
    internal const string GateMember = "gate";
    internal const string MonitorMember = "distributionProcessMonitors";
    internal const string LinksMember = "Links";
    internal const string UnwoundMember = "Unwound";
    internal const int Creation = 3;
    internal const int RemoteId = 7;
    internal const int MaximumProcesses = 5;
    internal const int HaltStatus = 17;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}

namespace Erlang.Tests;

internal static class ConnectedRemoteEpochHostNames449
{
    internal const string Test = "runtime/distribution449/remote-epoch-keeps-old-reference-and-rejects-stale-input";
    internal const string Receiver = "distribution449_receiver";
    internal const string MakeReference = "make_ref";
    internal const string Failure = "Remote node creation changed retained reference identity or accepted stale generation input";
    internal const uint PreviousCreation = 7;
    internal const uint NextCreation = 11;
}

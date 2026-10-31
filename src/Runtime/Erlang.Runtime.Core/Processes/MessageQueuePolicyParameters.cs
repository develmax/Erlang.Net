namespace Erlang;

internal static class MessageQueuePolicyParameters
{
    // CLR ownership transfer tuning; not an ERTS wire tag or fake reduction.
    internal const int EntriesPerBatch = 32;
    internal const string Invalid = "Invalid physical mailbox policy transition";
}

namespace Erlang;

internal readonly record struct MailboxSignalBatch428(int Processed, bool Deferred, bool Remaining)
{
    internal bool PendingBacking { get; init; }
}

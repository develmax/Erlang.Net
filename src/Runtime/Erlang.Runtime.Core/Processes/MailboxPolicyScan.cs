namespace Erlang;

// A pointer/revision scan cursor only, never native lifetime or authority.
internal sealed class MailboxPolicyScan
{
    internal object? AfterNode;
    internal long Revision = -1;
}

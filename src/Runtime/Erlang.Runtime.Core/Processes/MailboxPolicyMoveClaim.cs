namespace Erlang;

internal sealed class MailboxPolicyMoveClaim : IDisposable
{
    internal object Node { get; }
    internal Term Value { get; }
    internal SequenceTraceToken? Token { get; }
    internal Term? RawToken { get; }
    internal MailboxEntryCustody593 Custody { get; }
    internal MailboxPolicyScan Scan { get; }
    internal long ScanRevision { get; }
    private IDisposable? operation;

    internal MailboxPolicyMoveClaim(
        object node,
        Term value,
        MailboxEntryCustody593 custody,
        IDisposable operation,
        MailboxPolicyScan scan,
        long revision,
        SequenceTraceToken? token,
        Term? rawToken
    )
    {
        Node = node;
        Value = value;
        Token = token;
        RawToken = rawToken;
        Custody = custody;
        this.operation = operation;
        Scan = scan;
        ScanRevision = revision;
    }

    public void Dispose() => Interlocked.Exchange(ref operation, null)?.Dispose();
}

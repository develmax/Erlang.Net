namespace Erlang;

internal sealed class MailboxGuestCandidate593 : IDisposable
{
    private ProcessTermFrame506? frame;
    private IMailboxRootInterest593? protocolInterest601;
    private MailboxGraphSnapshot593? protocolGraph601;
    private bool protocol601;
    internal Term Value { get; }

    internal MailboxGuestCandidate593(Term value, MailboxGraphSnapshot593? graph)
    {
        protocolInterest601 = (IMailboxRootInterest593?)graph?.Retain();
        protocolGraph601 = graph; // Metadata borrowed under the exact retained interest.
        protocol601 = true;
        Value = value;
    }

    internal MailboxGuestCandidate593(ProcessContext context, Term value) : this(context, value, null) { }

    internal MailboxGuestCandidate593(ProcessContext context, Term value, MailboxGraphSnapshot593? graph)
    {
        frame = context.TermHeap506.Enter();
        try
        {
            Value = MailboxValueEscape.ImportGuestWithLiteral801(
                context.Runtime,
                frame,
                value,
                graph?.LiteralRoots801
            );
        }
        catch (Exception original)
        {
            try
            {
                Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    internal void AttachTo(object? selected)
    {
        if (selected is null)
            return;
        if (protocol601)
        {
            if (selected is not IProtocolMailboxSelection601 protocol)
                throw new InvalidOperationException(MailboxSnapshot601Names.InvalidAdmission);
            protocol.AttachSnapshotInterest601(protocolInterest601, protocolGraph601);
            protocolInterest601 = null;
            protocolGraph601 = null;

            return;
        }
        if (selected is not IGuestMailboxSelection593 owned)
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveSelection);

        owned.AttachMailboxCandidate593(frame ?? throw new ObjectDisposedException(nameof(MailboxGuestCandidate593)));
        frame = null;
    }

    public void Dispose()
    {
        Exception? first = null, second = null;
        try
        {
            Interlocked.Exchange(ref frame, null)?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            protocolGraph601 = null;
            MailboxRootSettlement593.Settle(Interlocked.Exchange(ref protocolInterest601, null)?.ReleaseLast());
        }
        catch (Exception error)
        {
            second = error;
        }
        if (first is not null && second is not null) throw new AggregateException(first, second);
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
        if (second is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(second).Throw();
    }
}

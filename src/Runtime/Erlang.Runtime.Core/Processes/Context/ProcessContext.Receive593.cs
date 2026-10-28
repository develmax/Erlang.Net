namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<T?> ReceiveSourceSelecting593<T>(ISourceMailboxSelector593<T> matcher, Term? timeout = null)
        where T : class
    {
        if (!ReferenceEquals(matcher.Context, this))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);
        MailboxSelectionAdmission593? admission = MailboxSelectionAdmission593.Source(this, matcher);

        return ReceiveAdmittedSelecting593(
            value => admission is null ? matcher.Select(value) : admission.InvokeSource(matcher, value), timeout, admission);
    }

    internal ValueTask<T?> ReceiveBeamPure593<T>(
        Func<Term, T?> select, Term? timeout, MailboxSelectionAdmission593? admission
    ) where T : class
        => ReceiveAdmittedSelecting593(value => ValueTask.FromResult(select(value)), timeout, admission);

    private async ValueTask<T?> ReceiveAdmittedSelecting593<T>(
        Func<Term, ValueTask<T?>> select, Term? timeout, MailboxSelectionAdmission593? admission
    ) where T : class
    {
        Runtime.ThrowIfHalted();
        T? selected = null;
        try
        {
            selected = await Mailbox.ReceiveGuestSelecting593(
                select,
                timeout,
                Cancellation,
                admission
            );
            Runtime.ThrowIfHalted();
            Cancellation.ThrowIfCancellationRequested();

            return selected;
        }
        catch (Exception primary)
        {
            try
            {
                (selected as IGuestMailboxSelection593)?.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            if (primary is OperationCanceledException && Runtime.IsHaltRequested)
                throw new ProcessHaltTransfer(Runtime);
            throw;
        }
    }

    internal void PublishSourceReceiveBindings593(IEnumerable<Term> values)
    {
        OwnedTermInvocation506? active = CurrentOwned506;
        if (active is not { CanBorrow507: true, IsDisposed: false }
            || active.BindingKind507 is not (OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest)
            || !active.OwnsContext507(this) || !RuntimeActorExecutionTurn.IsOwnedBy(this))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);

        foreach (Term value in values)
            active.Frame.AdoptExpression511(value);
    }
}

namespace Erlang;

internal sealed class MailboxSelectionAdmission593
{
    private readonly ProcessContext context;
    private readonly ProcessTermFrame506? anchor;
    private object? protocolMatcher601;
    private OwnedTermInvocation506? sourceOwner;
    private object? sourceMatcher;

    private MailboxSelectionAdmission593(ProcessContext context, ProcessTermFrame506? anchor)
    {
        this.context = context;
        this.anchor = anchor;
    }

    internal static MailboxSelectionAdmission593? Source(ProcessContext context, object matcher)
    {
        OwnedTermInvocation506? active = context.CurrentOwned506;
        if (!context.Runtime.MessageSnapshotRouteReady593 || !context.OwnedExecutionEnabled683 || active is not { CanBorrow507: true, IsDisposed: false }
            || active.BindingKind507 is not (OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest)
            || active.SourceEvaluationDepth511 == 0 || !active.OwnsContext507(context)
            || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            return null;

        return new(context, active.Frame) { sourceOwner = active, sourceMatcher = matcher };
    }

    internal static MailboxSelectionAdmission593 ProcessInfoReply601<T>(
        ProcessContext context, IProcessInfoMailboxSelector601<T> matcher) where T : class
    {
        if (!ReferenceEquals(matcher.Context, context))
            throw new InvalidOperationException(MailboxSnapshot601Names.InvalidAdmission);

        return new(context, null) { protocolMatcher601 = matcher };
    }

    internal ValueTask<T?> InvokeProcessInfo601<T>(IProcessInfoMailboxSelector601<T> matcher, Term value) where T : class
    {
        if (!ReferenceEquals(protocolMatcher601, matcher) || !ReferenceEquals(matcher.Context, context))
            throw new InvalidOperationException(MailboxSnapshot601Names.InvalidAdmission);

        return matcher.Select(value);
    }

    // Called only by the actual private BeamOwnedRoots507 instance holding its
    // registered frame. Public/opaque selectors never obtain this capability.
    internal static MailboxSelectionAdmission593? Beam(ProcessContext context, ProcessTermFrame506 frame)
    {
        if (!context.Runtime.MessageSnapshotRouteReady593 || !context.OwnedExecutionEnabled683)
            return null;

        if (!RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);

        using var validation = context.TermHeap506.Borrow(frame, Nil.Value);

        return new(context, frame);
    }

    internal ValueTask<T?> InvokeSource<T>(ISourceMailboxSelector593<T> matcher, Term candidate) where T : class
    {
        if (!ReferenceEquals(matcher, sourceMatcher) || !ReferenceEquals(matcher.Context, context))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);

        return matcher.Select(candidate);
    }

    internal ProcessExecutionAwaitable ResumeExecution()
        => protocolMatcher601 is null ? context.ResumeExecution()
            : new(ValueTask.CompletedTask, null, ProcessExecutionAdmission.Continuation);

    internal MailboxGuestCandidate593 Prepare(Term value, MailboxGraphSnapshot593? graph601 = null)
    {
        if (protocolMatcher601 is not null)
            return new MailboxGuestCandidate593(value, graph601);
        ObjectDisposedException.ThrowIf(anchor is null || anchor.IsDisposed, anchor);
        if (sourceOwner is not null && (!ReferenceEquals(context.CurrentOwned506, sourceOwner)
            || sourceOwner.IsDisposed || !sourceOwner.CanBorrow507 || !sourceOwner.OwnsContext507(context)))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);
        if (!RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new InvalidOperationException(MailboxCustody593Names.InvalidReceiveAdmission);

        return new(context, value, graph601);
    }
}

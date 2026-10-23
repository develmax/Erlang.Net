namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal ValueTask<DistributionGuestSendResult> SendConnectedNameAsync(
        ProcessContext producer,
        TupleTerm remote,
        Term payload,
        bool priority,
        bool noSuspend
    )
        => SendConnectedTargetAsync(
            producer,
            remote,
            payload,
            priority,
            noSuspend
        );

    internal ValueTask<DistributionGuestSendResult> SendConnectedPidAsync(
        ProcessContext producer,
        Pid remote,
        Term payload,
        bool priority,
        bool noSuspend
    )
        => SendConnectedTargetAsync(
            producer,
            remote,
            payload,
            priority,
            noSuspend
        );

    internal ValueTask<DistributionGuestSendResult> SendConnectedAliasAsync(
        ProcessContext producer,
        ReferenceTerm remote,
        Term payload,
        bool priority,
        bool noSuspend
    )
        => SendConnectedTargetAsync(
            producer,
            remote,
            payload,
            priority,
            noSuspend
        );

    private static string GuestTargetNode(Term target) => target switch
    {
        Pid pid => pid.Node,
        ReferenceTerm reference => reference.Node,
        TupleTerm { Items: [Atom, Atom node] } => node.Name,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    private bool GuestTargetHasForeignIdentity(Term target) => target switch
    {
        Pid pid => pid.LocalIdentity is not null && !ReferenceEquals(pid.LocalIdentity, runtime.LocalIdentity),
        ReferenceTerm reference => reference.LocalIdentity is not null && !ReferenceEquals(reference.LocalIdentity, runtime.LocalIdentity),
        TupleTerm { Items: [Atom, Atom] } => false,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    private static DistributionPassThroughMessage GuestAliasMessage(
        ulong flags,
        Pid sender,
        ReferenceTerm target,
        Term payload,
        bool priority,
        SequenceTraceToken? token
    )
    {
        if ((flags & DistributionPassThroughFlags.AlternativeActionSignal) != 0)
        {
            int actionFlags = DistributionAliasSendLayout.AlternativeAlias |
                (priority ? DistributionAlternativePidSendLayout.Priority : 0) |
                (token is not null ? DistributionAlternativePidSendLayout.Token : 0);

            return new(
                token is null
                ? Term.Tuple(
                    Term.I(DistributionControlOpcodes.AlternativeActionSend),
                    Term.I(actionFlags),
                    sender,
                    target
                )
                : Term.Tuple(
                    Term.I(DistributionControlOpcodes.AlternativeActionSend),
                    Term.I(actionFlags),
                    sender,
                    target,
                    token.ToTerm()
                ),
                payload
            );
        }

        return new(
            token is null
            ? Term.Tuple(Term.I(DistributionControlOpcodes.AliasSend), sender, target)
            : Term.Tuple(
                Term.I(DistributionControlOpcodes.AliasSendTrace),
                sender,
                target,
                token.ToTerm()
            ),
            payload
        );
    }
}

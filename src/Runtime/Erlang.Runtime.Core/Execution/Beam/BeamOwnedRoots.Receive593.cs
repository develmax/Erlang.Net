namespace Erlang;

internal sealed partial class BeamOwnedRoots507
{
    internal MailboxSelectionAdmission593? ReceiveAdmission593()
        => MailboxSelectionAdmission593.Beam(context, frame);

    internal PreparedSendArguments593 PrepareSend593(Term target, Term message)
        => ErlangOperatorValues.PrepareInternalSend593(
            context,
            frame,
            target,
            message
        );
}

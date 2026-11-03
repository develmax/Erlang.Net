namespace Erlang;

internal sealed partial class BeamOwnedRoots507
{
    internal MailboxSelectionAdmission593? ReceiveAdmission593()
    {
        RequireOpen553();

        return MailboxSelectionAdmission593.Beam(context, frame);
    }

    internal PreparedSendArguments593 PrepareSend593(Term target, Term message)
    {
        RequireOpen553();

        return ErlangOperatorValues.PrepareInternalSend593(
            context,
            frame,
            target,
            message
        );
    }
}

namespace Erlang;

internal sealed partial class BeamRegisterState
{
    internal MailboxSelectionAdmission593? ReceiveAdmission593() => roots507?.ReceiveAdmission593();

    internal PreparedSendArguments593 PrepareSend593(Term target, Term message)
        => roots507?.PrepareSend593(target, message) ?? new(target, message);
}

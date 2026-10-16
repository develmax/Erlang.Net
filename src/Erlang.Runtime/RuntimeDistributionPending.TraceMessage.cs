namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void DeliverConnectedTracePayload(TupleTerm control, Term payload)
    {
        int opcode = (int)((Integer)control.Items[DistributionControlOpcodes.OpcodeIndex]).Value;
        if (DistributionPassThroughCodec.IsAliasSend(control))
        {
            bool alternative = opcode == DistributionControlOpcodes.AlternativeActionSend;
            int actionFlags = alternative ? (int)((Integer)control.Items[DistributionAlternativePidSendLayout.FlagsIndex]).Value : 0;
            bool traced = alternative ? (actionFlags & DistributionAlternativePidSendLayout.Token) != 0
                : opcode == DistributionControlOpcodes.AliasSendTrace;
            runtime.SendConnectedAliasMessage(
                (ReferenceTerm)control.Items[alternative ? DistributionAlternativePidSendLayout.TargetIndex : DistributionAliasSendLayout.TargetIndex],
                payload,
                traced ? control.Items[alternative ? DistributionAlternativePidSendLayout.TokenIndex : DistributionAliasSendLayout.TokenIndex] : Nil.Value,
                control.Items[alternative ? DistributionAlternativePidSendLayout.SenderIndex : DistributionAliasSendLayout.SenderIndex],
                alternative && (actionFlags & DistributionAlternativePidSendLayout.Priority) != 0
            );

            return;
        }
        bool directSend = opcode is DistributionControlOpcodes.Send or DistributionControlOpcodes.SendTrace or
            DistributionControlOpcodes.SendSender or DistributionControlOpcodes.SendSenderTrace;
        Term target = control.Items[directSend
            ? DistributionPassThroughLayout.SendTargetIndex : DistributionPassThroughLayout.RegisteredTargetIndex];
        if (target is Atom name)
            target = runtime.WhereIs(name.Name);
        if (target is not Pid recipient || !runtime.IsCurrentLocal(recipient))
            return;
        Term token = opcode is DistributionControlOpcodes.SendTrace or DistributionControlOpcodes.SendSenderTrace
            ? control.Items[DistributionControlOpcodes.SendTraceTokenIndex]
            : opcode == DistributionControlOpcodes.RegisteredSendTrace
            ? control.Items[DistributionControlOpcodes.RegisteredSendTraceTokenIndex]
            : Nil.Value;
        runtime.SendConnectedTraceMessage(
            recipient,
            payload,
            token,
            control.Items[DistributionControlOpcodes.FromIndex]
        );
    }
}

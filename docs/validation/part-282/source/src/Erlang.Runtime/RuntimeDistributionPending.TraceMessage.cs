namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void DeliverConnectedTracePayload(TupleTerm control, Term payload)
    {
        int opcode = (int)((Integer)control.Items[DistributionControlOpcodes.OpcodeIndex]).Value;
        bool directSend = opcode is DistributionControlOpcodes.Send or DistributionControlOpcodes.SendTrace;
        Term target = control.Items[directSend
            ? DistributionPassThroughLayout.SendTargetIndex : DistributionPassThroughLayout.RegisteredTargetIndex];
        if (target is Atom name)
            target = runtime.WhereIs(name.Name);
        if (target is not Pid recipient || !runtime.IsCurrentLocal(recipient))
            return;
        Term token = opcode == DistributionControlOpcodes.SendTrace
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

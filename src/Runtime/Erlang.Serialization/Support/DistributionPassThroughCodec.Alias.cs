namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    public static bool IsAliasSend(TupleTerm control)
    {
        if (control.Items.Count == 0 || control.Items[0] is not Integer opcode)
            return false;

        return opcode.Value == DistributionControlOpcodes.AliasSend || opcode.Value == DistributionControlOpcodes.AliasSendTrace ||
            opcode.Value == DistributionControlOpcodes.AlternativeActionSend && control.Items.Count > DistributionAlternativePidSendLayout.FlagsIndex &&
            control.Items[DistributionAlternativePidSendLayout.FlagsIndex] is Integer flags &&
            (flags.Value & DistributionAliasSendLayout.AlternativeAlias) != 0;
    }

    private static void ValidateAliasSend(TupleTerm control, ulong negotiatedFlags)
    {
        if ((negotiatedFlags & DistributionPassThroughFlags.Alias) == 0)
            throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
        bool alternative = ((Integer)control.Items[0]).Value == DistributionControlOpcodes.AlternativeActionSend;
        if (alternative)
        {
            if ((negotiatedFlags & DistributionPassThroughFlags.AlternativeActionSignal) == 0)
                throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
            if (control.Items[DistributionAlternativePidSendLayout.FlagsIndex] is not Integer flags || flags.Value < 0 ||
                flags.Value > DistributionAliasSendLayout.AlternativeSupportedFlags ||
                control.Items.Count != (((int)flags.Value & DistributionAlternativePidSendLayout.Token) != 0
                    ? DistributionAlternativePidSendLayout.TraceArity : DistributionAlternativePidSendLayout.Arity) ||
                control.Items[DistributionAlternativePidSendLayout.SenderIndex] is not Pid ||
                control.Items[DistributionAlternativePidSendLayout.TargetIndex] is not ReferenceTerm)
                throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        }
        else if (control.Items.Count != (((Integer)control.Items[0]).Value == DistributionControlOpcodes.AliasSendTrace
            ? DistributionAliasSendLayout.TraceArity : DistributionAliasSendLayout.Arity) ||
            control.Items[DistributionAliasSendLayout.SenderIndex] is not Pid ||
            control.Items[DistributionAliasSendLayout.TargetIndex] is not ReferenceTerm)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
    }
}

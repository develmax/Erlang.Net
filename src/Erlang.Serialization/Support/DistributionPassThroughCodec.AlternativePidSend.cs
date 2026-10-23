namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    private static void ValidateAlternativePidSend(TupleTerm control, ulong negotiatedFlags)
    {
        if ((negotiatedFlags & DistributionPassThroughFlags.AlternativeActionSignal) == 0)
            throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
        var items = control.Items;
        if (items.Count is not (DistributionAlternativePidSendLayout.Arity or DistributionAlternativePidSendLayout.TraceArity) ||
            items[DistributionAlternativePidSendLayout.FlagsIndex] is not Integer flags || flags.Value < 0 ||
            flags.Value > DistributionAlternativePidSendLayout.SupportedFlags ||
            items[DistributionAlternativePidSendLayout.SenderIndex] is not Pid ||
            items[DistributionAlternativePidSendLayout.TargetIndex] is not Pid ||
            items.Count != (((int)flags.Value & DistributionAlternativePidSendLayout.Token) != 0
                ? DistributionAlternativePidSendLayout.TraceArity : DistributionAlternativePidSendLayout.Arity))
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
        if (items.Count == DistributionAlternativePidSendLayout.TraceArity &&
            items[DistributionAlternativePidSendLayout.TokenIndex] is not TupleTerm { Items.Count: DistributionAlternativePidSendLayout.TraceTokenArity })
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
    }
}

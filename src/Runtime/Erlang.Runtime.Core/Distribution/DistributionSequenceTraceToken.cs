namespace Erlang;

internal static class DistributionSequenceTraceToken
{
    internal static DistributionSequenceTraceReception Decode(Term raw)
    {
        if (raw is Nil)
            return new(null, false);
        if (raw is Atom { Name: DistributionSequenceTraceNames.PreserveMarker })
            return new(null, true);
        if (raw is not TupleTerm { Items.Count: DistributionSequenceTraceLayout.Arity } tuple ||
            tuple.Items[DistributionSequenceTraceLayout.FlagsIndex] is not Integer flags ||
            tuple.Items[DistributionSequenceTraceLayout.SerialIndex] is not Integer serial ||
            tuple.Items[DistributionSequenceTraceLayout.LastCountIndex] is not Integer last ||
            tuple.Items[DistributionSequenceTraceLayout.SenderIndex] is not (Pid or Atom) ||
            !Small(flags) || !Small(serial) || !Small(last))
            throw new NotSupportedException(DistributionSequenceTraceNames.UnsupportedToken);

        return new(
            new SequenceTraceToken(
                (SequenceTraceFlags)(long)flags.Value,
                tuple.Items[DistributionSequenceTraceLayout.LabelIndex],
                (long)serial.Value,
                tuple.Items[DistributionSequenceTraceLayout.SenderIndex],
                (long)last.Value
            ),
            false
        );
    }

    private static bool Small(Integer value)
        => value.Value >= DistributionSequenceTraceLayout.MinimumSmallInteger &&
            value.Value <= DistributionSequenceTraceLayout.MaximumSmallInteger;
}

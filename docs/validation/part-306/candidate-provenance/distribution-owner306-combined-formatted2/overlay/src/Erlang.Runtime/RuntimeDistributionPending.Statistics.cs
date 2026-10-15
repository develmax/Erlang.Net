namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal Term GetStatistics(Term handle)
    {
        lock (gate)
        {
            try
            {
                var entry = ReadHandle(handle, out uint connectionId, out var lease);
                using (lease)
                    return Statistics(entry, connectionId);
            }
            catch (ArgumentException)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    private Term Statistics(RuntimeDistributionPendingEntry entry, uint connectionId)
    {
        if (entry.ConnectionId != connectionId || entry.State == DistributionPendingState.Closed)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (entry.Connected is not { } connected)
            return Term.Tuple(
                Term.A(CommonAtomNames.Ok),
                Term.I(0),
                Term.I(0),
                Term.I(0)
            );
        var snapshot = connected.Queue.AccountedStatistics(connected.QueueHandle);
        if (snapshot.Retired)
            return Term.Tuple(
                Term.A(CommonAtomNames.Ok),
                Term.I(0),
                Term.I(0),
                Term.I(0)
            );

        return Term.Tuple(
            Term.A(CommonAtomNames.Ok),
            Term.I(connected.RawInputAttempts),
            Term.I(snapshot.OutputBuffers),
            Term.I(snapshot.QueuedBytes)
        );
    }
}

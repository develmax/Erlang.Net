namespace Erlang;

internal sealed unsafe partial class OrdinaryBinaryBacking
{
    // Selected-event transition only. The genuine source operation still owns
    // its existing BinRef, so rollback cannot become the last native release.
    internal OrdinaryBinaryLease RetainReadonlyReserved817(OrdinaryBinaryLease lease)
    {
        if (!ReferenceEquals(lease.Owner, this))
            throw new InvalidOperationException(OrdinaryBinaryNames.Prepared);
        lock (gate)
        {
            var header = (OrdinaryBinaryHeader*)Require();
            if (header->Flags != 0 || writableBinding is not null)
                throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);
            header->References = checked(header->References + 1);
            if (!lease.TryAdmit())
            {
                header->References--;
                throw new ObjectDisposedException(nameof(OrdinaryBinaryLease));
            }
        }

        return lease;
    }
}

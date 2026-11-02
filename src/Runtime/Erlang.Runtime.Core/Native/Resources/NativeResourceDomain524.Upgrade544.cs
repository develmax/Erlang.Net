namespace Erlang;

internal sealed partial class NativeResourceDomain524
{
    internal void RequireUpgradeAdmission544()
    {
        lock (gate)
            ObjectDisposedException.ThrowIf(closing, NativeResource524Names.Closed);
    }

    internal NativeResourceType524 CreateUpgradePendingType544(
        string name,
        NifNativeResourceImage524 image,
        NativeResourceLoadTransaction527 transaction,
        NativeResourceUpgradeSnapshot544 snapshot
    )
    {
        lock (gate)
        {
            // Closing is permitted only for this exact active admitted load.
            if (pendingLoads527 == 0 || !snapshot.IsActiveFor544(transaction, this, image))
                throw new InvalidOperationException(NativeResourceUpgradeSnapshotNames544.Authority);
            var type = new NativeResourceType524(
                name,
                image,
                0,
                0,
                0,
                0
            );
            try
            {
                types.Add(type);
            }
            catch
            {
                type.Dispose();
                throw;
            }

            return type;
        }
    }
}

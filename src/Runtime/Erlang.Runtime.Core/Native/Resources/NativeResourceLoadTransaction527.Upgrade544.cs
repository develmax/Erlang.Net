namespace Erlang;

internal sealed partial class NativeResourceLoadTransaction527
{
    private NativeResourceUpgradeSnapshot544? upgradeSnapshot544;
    internal NativeResourceUpgradeSnapshot544? UpgradeSnapshot544 => upgradeSnapshot544;
    private int UpgradeFailureCapacity544 => upgradeSnapshot544?.FailureCapacity544 ?? 0;

    internal NativeResourceUpgradeCommit544 PrepareUpgradeCommit544()
    {
        // Report an actual domain close before validating a fresh operation's
        // pre-callback shape. The in-writer check remains its admission point.
        domain.RequireUpgradeAdmission544();
        if (completed || !loading || opens.Count != 0 || upgradeSnapshot544 is not null)
            throw new InvalidOperationException(NativeResourceUpgradeSnapshotNames544.Repeated);
        NativeResourceUpgradeSnapshot544? snapshot = null;
        try
        {
            table.EnterPublication529();
            try
            {
                table.ValidatePublication527();
                domain.RequireUpgradeAdmission544();
                snapshot = table.CreateUpgradeSnapshot544(this, image, authority);
                snapshot.RetainUnderPublication544();
                var failures = new Exception?[checked(snapshot.FailureCapacity544
                    + NativeResourcePublication527Parameters.FinalFailures)];
                var result = NativeResourceUpgradeCommit544.Create544(this, table, publication527);
                publicationFailures527 = failures;
                upgradeSnapshot544 = snapshot;

                return result;
            }
            finally
            {
                table.LeavePublication529();
            }
        }
        catch (Exception failure)
        {
            snapshot?.Release544();
            if (snapshot is not null)
            {
                var errors = new List<Exception> { failure };
                foreach (var error in snapshot.Failures544)
                    if (error is not null) errors.Add(error);
                if (errors.Count != 1) throw new AggregateException(errors);
            }
            throw;
        }
    }
}

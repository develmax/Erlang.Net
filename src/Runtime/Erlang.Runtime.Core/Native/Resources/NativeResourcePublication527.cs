namespace Erlang;

internal sealed class NativeResourcePublication527(
    NativeResourceLoadTransaction527 transaction,
    NativeResourceTypeTable527 table,
    List<NativeResourceTypeOpen527> opens
) : IDisposable
{
    private bool admitted, committed;
    private int count527;

    internal void Seal527(int count) => count527 = count;

    internal void Admit527() => admitted = true;

    internal void Commit527()
    {
        table.Commit527(opens, count527);
        committed = true;
    }

    internal void CommitAuthorizedUpgrade544(bool imageRetired)
    {
        table.CommitAuthorizedUpgrade544(opens, count527, imageRetired);
        committed = true;
    }

    public void Dispose()
    {
        if (!admitted) return;
        admitted = false;
        table.LeavePublication529();
        if (committed) transaction.FinishPublication527(count527);
    }
}

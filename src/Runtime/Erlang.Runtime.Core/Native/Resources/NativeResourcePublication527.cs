namespace Erlang;

internal sealed class NativeResourcePublication527(
    NativeResourceLoadTransaction527 transaction,
    NativeResourceTypeTable527 table,
    NativeResourceTypeOpen527[] opens
) : IDisposable
{
    private bool admitted, committed;

    internal void Admit527() => admitted = true;

    internal void Commit527()
    {
        table.Commit527(opens);
        committed = true;
    }

    public void Dispose()
    {
        if (!admitted) return;
        admitted = false;
        table.LeavePublication529();
        if (committed) transaction.FinishPublication527(opens);
    }
}

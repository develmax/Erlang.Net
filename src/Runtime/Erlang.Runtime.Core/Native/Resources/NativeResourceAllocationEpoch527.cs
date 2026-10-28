namespace Erlang;

internal readonly struct NativeResourceAllocationEpoch527 : IDisposable
{
    private readonly NativeResourceTypeTable527? table;

    internal NativeResourceAllocationEpoch527(NativeResourceTypeTable527? table)
    {
        this.table = table;
        table?.EnterRead527();
    }

    public void Dispose() => table?.LeaveRead527();
}

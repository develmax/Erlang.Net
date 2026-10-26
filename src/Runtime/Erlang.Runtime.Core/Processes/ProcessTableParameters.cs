namespace Erlang;

internal static class ProcessTableParameters
{
    public const int DefaultCapacity = 1 << 20;
    public const int MaximumCapacity = (1 << 27) - 1;
    public const int InitialCursor = 0;
    public const int NativeIndexesPerReduction = 25;
    public const int ManagedBatchPidBudget = 1000;
    public const int ManagedScanWindow = ManagedBatchPidBudget * NativeIndexesPerReduction;
}

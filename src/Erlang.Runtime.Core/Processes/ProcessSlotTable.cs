namespace Erlang;

internal sealed partial class ProcessSlotTable
{
    private readonly List<Pid?> slots = new();
    internal long AllocatedPayloadBytes => (long)slots.Capacity * IntPtr.Size;
    internal long UsedPayloadBytes => (long)identities.Count * IntPtr.Size;
    private readonly Dictionary<Pid, int> identities = new();
    private readonly SortedSet<int> occupied = new();
    private readonly Queue<int> released = new();
    private int nextSlot;

    public int Capacity { get; }

    public ProcessSlotTable(int capacity)
    {
        if (capacity < 1 || capacity > ProcessTableParameters.MaximumCapacity)
            throw new ArgumentOutOfRangeException(nameof(capacity), ProcessTableDiagnostics.InvalidCapacity);
        Capacity = capacity;
    }

    public void RequireCapacity()
    {
        if (identities.Count == Capacity)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
    }

    public void Add(Pid pid)
    {
        RequireCapacity();
        int slot = released.TryDequeue(out int reused) ? reused : nextSlot++;
        if (slot == slots.Count)
            slots.Add(pid);
        else
            slots[slot] = pid;
        identities.Add(pid, slot);
        occupied.Add(slot);
    }

    public void Remove(Pid pid)
    {
        if (!identities.Remove(pid, out int slot))
            return;
        slots[slot] = null;
        occupied.Remove(slot);
        released.Enqueue(slot);
    }

    public ProcessTableBatch? Read(int cursor)
    {
        if (cursor < 0 || cursor > Capacity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (cursor == Capacity)
            return null;
        int limit = Math.Min(Capacity, cursor + ProcessTableParameters.ManagedScanWindow);
        var processes = new List<Pid>(Math.Min(identities.Count, ProcessTableParameters.ManagedBatchPidBudget));
        int next = limit;
        foreach (int slot in occupied.GetViewBetween(cursor, limit - 1))
        {
            processes.Add(slots[slot]!);
            if (processes.Count == ProcessTableParameters.ManagedBatchPidBudget)
            {
                next = slot + 1;
                break;
            }
        }

        return new ProcessTableBatch(next, processes.AsReadOnly());
    }
}

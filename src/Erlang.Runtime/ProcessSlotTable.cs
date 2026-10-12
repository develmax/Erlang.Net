namespace Erlang;

internal sealed class ProcessSlotTable
{
    private readonly Dictionary<int, Pid> slots = new();
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
        if (slots.Count == Capacity)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
    }

    public void Add(Pid pid)
    {
        RequireCapacity();
        int slot = released.TryDequeue(out int reused) ? reused : nextSlot++;
        slots.Add(slot, pid);
        identities.Add(pid, slot);
        occupied.Add(slot);
    }

    public void Remove(Pid pid)
    {
        if (!identities.Remove(pid, out int slot))
            return;
        slots.Remove(slot);
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
        var processes = new List<Pid>(Math.Min(slots.Count, ProcessTableParameters.ManagedBatchPidBudget));
        int next = limit;
        foreach (int slot in occupied.GetViewBetween(cursor, limit - 1))
        {
            processes.Add(slots[slot]);
            if (processes.Count == ProcessTableParameters.ManagedBatchPidBudget)
            {
                next = slot + 1;
                break;
            }
        }

        return new ProcessTableBatch(next, processes.AsReadOnly());
    }
}

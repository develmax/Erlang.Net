namespace Erlang;

internal sealed partial class ProcessSlotTable
{
    internal ProcessTableBatch? ReadNative419(int cursor, int maximumPids)
    {
        if (cursor < 0 || cursor > Capacity) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (cursor == Capacity) return null;
        int limit = (int)Math.Min(Capacity, (long)cursor + maximumPids * ProcessTableParameters.NativeIndexesPerReduction);
        var result = new List<Pid>(Math.Min(identities.Count, maximumPids));
        int next = limit;
        foreach (int slot in occupied.GetViewBetween(cursor, limit - 1))
        {
            result.Add(slots[slot]!);
            if (result.Count == maximumPids)
            {
                next = slot + 1;
                break;
            }
        }

        return new(next, result.AsReadOnly());
    }
}

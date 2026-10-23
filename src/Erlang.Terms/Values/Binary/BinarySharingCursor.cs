namespace Erlang;

internal sealed class BinarySharingCursor
{
    private readonly Stack<(Term Value, long Child)> pending = new();
    private readonly HashSet<Term> seen = new(ReferenceEqualityComparer.Instance);

    internal BinarySharingCursor(Term value) => pending.Push((value, -1));

    internal bool Step(int work, CancellationToken cancellation = default)
    {
        if (work <= 0)
            throw new ArgumentOutOfRangeException(nameof(work));
        while (work-- > 0 && pending.TryPop(out var frame))
        {
            cancellation.ThrowIfCancellationRequested();
            var value = frame.Value;
            long child = frame.Child;
            if (child < 0)
            {
                if (!seen.Add(value))
                    continue;
                if (value is BitString binary)
                {
                    binary.PinForSharing();
                    continue;
                }
                child = 0;
            }
            Term? next = value switch
            {
                Cons cell => child switch
                {
                    0 => cell.Head,
                    1 => cell.Tail,
                    _ => null
                },
                TupleTerm tuple when child < tuple.Items.Count => tuple.Items[(int)child],
                NativeRecordTerm record when child < record.Values.Count => record.Values[(int)child],
                FunctionTerm function when child < function.Environment.Count => function.Environment[(int)child],
                MapTerm map when child / 2 < map.Entries.Count => child % 2 == 0 ? map.Entries[(int)(child / 2)].Key : map.Entries[(int)(child / 2)].Value,
                _ => null
            };
            if (next is not null)
            {
                pending.Push((value, child + 1));
                pending.Push((next, -1));
            }
        }

        return pending.Count == 0;
    }

    internal static void Pin(Term value, CancellationToken cancellation = default)
    {
        var cursor = new BinarySharingCursor(value);
        while (!cursor.Step(BinarySharingParameters.WorkChunk, cancellation)) { }
    }
}

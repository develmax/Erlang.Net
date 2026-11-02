namespace Erlang;

internal sealed class RuntimeWallTimeObservation(int normalCount)
{
    internal RuntimeManagedSchedulerWallTimeSample?[] Normal { get; } = new RuntimeManagedSchedulerWallTimeSample?[normalCount];
    internal RuntimeDirtyWallTimeSample[]? Dirty { get; set; }

    internal Term ToTerm()
    {
        if (Normal.Any(sample => sample is null)) return Term.A(CommonAtomNames.Undefined);
        RuntimeDirtyWallTimeSample[] dirty = Dirty ?? throw new InvalidOperationException(nameof(RuntimeWallTimeObservation));
        var values = new List<Term>(Normal.Length + dirty.Length);
        foreach (RuntimeManagedSchedulerWallTimeSample? lane in Normal)
            values.Add(Term.Tuple(Term.I(lane!.Lane), Term.I(lane.ActiveTicks), Term.I(lane.TotalTicks)));
        for (int index = 0; index < dirty.Length; index++)
            values.Add(Term.Tuple(Term.I(Normal.Length + index + 1), Term.I(dirty[index].Active), Term.I(dirty[index].Total)));

        return Cons.From(values);
    }
}

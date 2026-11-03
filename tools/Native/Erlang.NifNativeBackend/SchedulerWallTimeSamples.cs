namespace Erlang.NifNativeBackend;

internal static class SchedulerWallTimeSamples
{
    internal static Dictionary<int, (long Active, long Total)> Read(Term value)
    {
        var result = new Dictionary<int, (long Active, long Total)>();
        foreach (Term item in Cons.Items(value))
        {
            CpuStatisticsAccess.Require(item is TupleTerm { Items.Count: 3 });
            var tuple = (TupleTerm)item;
            int id = checked((int)((Integer)tuple.Items[0]).Value);
            long active = checked((long)((Integer)tuple.Items[1]).Value);
            long total = checked((long)((Integer)tuple.Items[2]).Value);
            CpuStatisticsAccess.Require(id > 0 && active >= 0 && total >= active && result.TryAdd(id, (active, total)));
        }

        return result;
    }

    internal static void Boolean(Term value, bool expected)
        => CpuStatisticsAccess.Require(value.Equals(CoreModules.Boolean(expected)));

    internal static void Undefined(Term value)
        => CpuStatisticsAccess.Require(value.Equals(Term.A(ProcessRegistryAtoms.Undefined)));
}

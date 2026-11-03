namespace Erlang;

public sealed partial class MapTerm
{
    private static bool HasStrictArithmeticOrder(
        IReadOnlyList<KeyValuePair<Term, Term>> entries,
        ref NodeComparisonContext context
    )
    {
        for (int index = 1; index < entries.Count; index++)
            if (TermOrder.CompareSnapshot(
                entries[index - 1].Key,
                entries[index].Key,
                true,
                ref context
            ) >= 0)
                return false;

        return true;
    }
}

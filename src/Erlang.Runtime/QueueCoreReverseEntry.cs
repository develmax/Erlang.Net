namespace Erlang;

internal readonly record struct QueueCoreReverseEntry(
    Term List,
    Term Tail,
    int Reductions
)
{
    internal long CellBudget => (long)Reductions * QueueCoreReverseParameters.CellsPerReduction;
}

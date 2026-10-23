namespace Erlang;

internal sealed class TermHashFrame(IEnumerator<TermHashStep> steps)
{
    internal IEnumerator<TermHashStep> Steps { get; } = steps;
    internal HashCode Accumulator;
    internal TermHashStepKind PendingKind;
    private int mapKey;
    private int mapPairs;

    internal void Consume(TermHashStepKind kind, int hash)
    {
        switch (kind)
        {
            case TermHashStepKind.MapKey:
                mapKey = hash;
                break;
            case TermHashStepKind.MapValue:
                mapPairs = unchecked(mapPairs + HashCode.Combine(mapKey, hash));
                break;
            case TermHashStepKind.MapPairs:
                Accumulator.Add(mapPairs);
                break;
            default:
                Accumulator.Add(hash);
                break;
        }
    }
}

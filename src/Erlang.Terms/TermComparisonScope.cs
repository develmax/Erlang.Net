namespace Erlang;

internal sealed class TermComparisonScope(
    Term left,
    Term right,
    bool exact,
    TermComparisonPurpose purpose,
    TermComparisonResult? capture = null
) : IDisposable
{
    internal TermComparisonStep Current { get; set; } = TermComparisonStep.Pair(left, right, exact);
    internal TermComparisonPurpose Purpose { get; } = purpose;
    internal TermComparisonResult? Capture { get; } = capture;
    internal Stack<IEnumerator<TermComparisonStep>> Pending { get; } = new();

    internal bool MoveNext()
    {
        while (Pending.Count > 0)
        {
            var sequence = Pending.Peek();
            if (sequence.MoveNext())
            {
                Current = sequence.Current;

                return true;
            }
            Pending.Pop().Dispose();
        }

        return false;
    }

    public void Dispose()
    {
        while (Pending.TryPop(out var sequence))
            sequence.Dispose();
    }
}

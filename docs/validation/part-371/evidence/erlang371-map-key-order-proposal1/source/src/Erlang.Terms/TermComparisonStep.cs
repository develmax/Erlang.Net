namespace Erlang;

internal readonly record struct TermComparisonStep(
    Term? Left,
    Term? Right,
    bool Exact,
    int Result,
    TermComparisonResult? Capture = null,
    TermComparisonPurpose? Purpose = null
)
{
    internal static TermComparisonStep Pair(Term left, Term right, bool exact) => new(left, right, exact, 0);

    internal static TermComparisonStep Scalar(int result) => new(null, null, false, result);

    internal static TermComparisonStep Observe(
        Term left,
        Term right,
        bool exact,
        TermComparisonPurpose purpose,
        TermComparisonResult capture
    ) => new(left, right, exact, 0, capture, purpose);
}

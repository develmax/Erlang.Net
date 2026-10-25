namespace Erlang;

internal readonly record struct TermHashStep(Term? Value, int Hash, TermHashStepKind Kind)
{
    internal static TermHashStep Child(Term value, TermHashStepKind kind = TermHashStepKind.Add) => new(value, 0, kind);

    internal static TermHashStep Scalar(int hash) => new(null, hash, TermHashStepKind.Add);
}

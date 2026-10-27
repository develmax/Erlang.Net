using System.Runtime.CompilerServices;

namespace Erlang;

// Indexes metadata views by object occurrence identity, never Erlang equality.
internal sealed class NativeMapRowReferenceComparer510 : IEqualityComparer<KeyValuePair<Term, Term>>
{
    internal static NativeMapRowReferenceComparer510 Instance { get; } = new();

    public bool Equals(KeyValuePair<Term, Term> first, KeyValuePair<Term, Term> second)
        => ReferenceEquals(first.Key, second.Key) && ReferenceEquals(first.Value, second.Value);

    public int GetHashCode(KeyValuePair<Term, Term> pair)
        => HashCode.Combine(RuntimeHelpers.GetHashCode(pair.Key), RuntimeHelpers.GetHashCode(pair.Value));
}

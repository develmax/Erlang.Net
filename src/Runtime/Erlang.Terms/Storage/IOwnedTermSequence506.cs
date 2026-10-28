namespace Erlang;

internal interface IOwnedTermSequence506 : IReadOnlyList<Term>
{
    object Owner { get; }
    long Identity { get; }
}

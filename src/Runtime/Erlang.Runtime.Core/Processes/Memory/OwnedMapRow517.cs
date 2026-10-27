namespace Erlang;

// Offset of one physical occurrence; equal keys and repeated object identities
// remain distinct rows and can carry different values.
internal readonly record struct OwnedMapRow517(
    ProcessTermSequence506 Keys,
    int KeyIndex,
    ProcessTermSequence506 Values,
    int ValueIndex
)
{
    internal KeyValuePair<Term, Term> Read() => new(Keys[KeyIndex], Values[ValueIndex]);
}

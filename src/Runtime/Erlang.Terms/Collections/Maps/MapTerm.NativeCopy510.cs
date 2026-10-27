namespace Erlang;

public sealed partial class MapTerm
{
    internal static MapTerm FromCopiedNativeLayout510(
        KeyValuePair<Term, Term>[] physical,
        KeyValuePair<Term, Term>[] canonical,
        KeyValuePair<Term, Term>[] arithmetic,
        TupleTerm? flatKeys,
        Func<Term, (bool Found, Term? Value)>? nativeLookup,
        Func<MapTerm, NativeMapEqualityPlan505?> nativeEquality
    ) => new(
        physical,
        canonical,
        arithmetic,
        flatKeys,
        nativeLookup,
        nativeEquality
    );

    private MapTerm(
        KeyValuePair<Term, Term>[] physical,
        KeyValuePair<Term, Term>[] canonical,
        KeyValuePair<Term, Term>[] arithmetic,
        TupleTerm? flatKeys,
        Func<Term, (bool Found, Term? Value)>? nativeLookup,
        Func<MapTerm, NativeMapEqualityPlan505?> nativeEquality
    )
    {
        // Lookup is only the existing flat-map shadow. It never supplies or
        // normalizes either physical topology or copied key/value occurrences.
        lookup = new();
        foreach (var pair in physical)
            lookup.TryAdd(pair.Key, pair.Value);
        Entries = Array.AsReadOnly((KeyValuePair<Term, Term>[])canonical.Clone());
        ArithmeticKeyEntries = Array.AsReadOnly((KeyValuePair<Term, Term>[])arithmetic.Clone());
        FlatKeys426 = flatKeys;
        nativeLookup505 = nativeLookup;
        NativeEquality505 = nativeEquality;
    }
}

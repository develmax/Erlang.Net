namespace Erlang;

public sealed partial class MapTerm
{
    internal IOwnedMapPayload517? OwnedMapPayload517 { get; }

    internal static MapTerm FromOwnedPayload517(
        IOwnedMapPayload517 payload,
        TupleTerm? flatKeys,
        Func<Term, (bool Found, Term? Value)> lookup,
        Func<MapTerm, NativeMapEqualityPlan505?> equality,
        bool logicalOrigin
    ) => new(
        payload,
        flatKeys,
        lookup,
        equality,
        logicalOrigin
    );

    private MapTerm(
        IOwnedMapPayload517 payload,
        TupleTerm? flatKeys,
        Func<Term, (bool Found, Term? Value)> nativeLookup,
        Func<MapTerm, NativeMapEqualityPlan505?> nativeEquality,
        bool logicalOrigin
    )
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(nativeLookup);
        ArgumentNullException.ThrowIfNull(nativeEquality);
        // Deliberately empty: a second managed key/value dictionary would
        // retain boxed children independently of the registered heap slots.
        lookup = new();
        IsOrdinaryLogicalMap517 = logicalOrigin;
        OwnedMapPayload517 = payload;
        Entries = payload.Canonical;
        ArithmeticKeyEntries = payload.Arithmetic;
        FlatKeys426 = flatKeys;
        nativeLookup505 = nativeLookup;
        NativeEquality505 = nativeEquality;
    }
}

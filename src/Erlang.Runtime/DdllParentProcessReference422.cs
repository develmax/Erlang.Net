namespace Erlang;

// Multiplicity is real: two loads by one PID remain two entries. During reload
// a loaded entry may be dereferenced while retaining its source owner relation.
internal sealed record DdllParentProcessReference422(
    Pid Owner,
    DdllParentReferenceKind422 Kind,
    ReferenceTerm? Monitor = null,
    bool Deferred = false
);

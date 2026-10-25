namespace Erlang;

internal sealed record ProcessInfoDefaultProjection(
    bool Exists,
    IReadOnlyList<Term> AvailableFields,
    IReadOnlyList<string> UnavailableMandatoryFields
);

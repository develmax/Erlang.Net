namespace Erlang;

internal interface IOwnedNativeRecordStorage : IOwnedTermSequence506
{
    TupleTerm ReadRecordDefinition();

    Atom ReadRecordModule();

    Atom ReadRecordName();

    bool ReadRecordExported();

    IReadOnlyList<Atom> ReadRecordFieldNames();

    IReadOnlyList<Term> ReadRecordValues();
}

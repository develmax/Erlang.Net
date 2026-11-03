namespace Erlang;

internal interface IOwnedExternalIdentifierStorage : IOwnedTermSequence506
{
    LocalNodeSnapshot ReadExternalIdentity();

    ulong ReadExternalId();

    IReadOnlyList<uint> ReadExternalReferenceWords();
}

namespace Erlang;

internal readonly record struct ProcessOwnedWordSnapshot504(
    int WordBytes,
    long ReservedWords,
    long RootedWords,
    int ActiveLeases,
    bool Closing
);

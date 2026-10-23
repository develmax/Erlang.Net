namespace Erlang;

internal sealed record PersistentArenaNode416(
    PersistentArenaKind416 Kind,
    byte[] Data,
    Term[] Children,
    int Root = PersistentArenaNames416.MissingRoot,
    long Auxiliary = 0
);

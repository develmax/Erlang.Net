namespace Erlang;

internal enum BeamBinaryCommandKind
{
    EnsureAtLeast,
    EnsureExactly,
    Skip,
    Equal,
    Integer,
    Binary,
    Tail
}

namespace Erlang;

internal readonly record struct OwnedPhysicalWordSnapshot526(
    long ReservedWords,
    long LiveTermCells,
    long LiveEncodedHeaderWords,
    long LivePhysicalWords,
    int Blocks,
    long RootedTermHandles,
    long LiveRawNumericWords556 = 0
);

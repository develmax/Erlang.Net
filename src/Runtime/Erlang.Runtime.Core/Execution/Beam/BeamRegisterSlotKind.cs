namespace Erlang;

internal enum BeamRegisterSlotKind
{
    Uninitialized,
    Term,
    Handler,
    SavedTrace,
    ReceiveMarker,
    BinaryMatchContext,
    BinaryMatchPosition
}

namespace Erlang.Tests;

internal enum BeamCatchProtocolFault
{
    CatchAsTryEnd,
    CatchAsTryCase,
    TryAsCatchEnd,
    WrongSlot,
    TagEscape,
    LiveTagReturn,
    TrimDropsTag,
    NormalTryHandlerEntry,
    PendingRawEscape,
    PendingHandlerOverwrite
}

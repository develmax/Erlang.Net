namespace Erlang.Tests;

internal enum BeamClosureProtocolShape
{
    AllRaise,
    OneReturns,
    DefaultReturns,
    EntryCycle,
    LocalCycle,
    CaughtReturn,
    SharedDag
}

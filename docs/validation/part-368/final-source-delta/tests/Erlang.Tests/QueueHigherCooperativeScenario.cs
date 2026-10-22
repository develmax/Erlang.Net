namespace Erlang.Tests;

internal enum QueueHigherCooperativeScenario
{
    FilterFrontRebuild,
    FilterRearUnwind,
    DeleteFrontRebuild,
    DeleteRearRebuild,
    DeleteFrontNoMatch,
    DeleteRearNoMatch,
    Split,
    Join
}

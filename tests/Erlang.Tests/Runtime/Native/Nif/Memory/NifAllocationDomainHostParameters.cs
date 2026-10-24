namespace Erlang.Tests;

internal static class NifAllocationDomainHostParameters
{
    internal const int FirstBlockBytes = 13;
    internal const int SecondBlockBytes = 23;
    internal const int LaterBlockBytes = 7;
    internal const int ConcurrentIterations = 128;
    internal const int ConcurrentBlockLimit = 31;
    internal const int SimultaneousOwners = 2;
    internal const int PayloadLiveBlocks = 2;
    internal const byte PayloadMarker = 0x69;
}

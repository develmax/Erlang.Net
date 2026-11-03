namespace Erlang.Tests;

internal static class Allocation567TestNames
{
    internal const string ScalarCase = "runtime/allocation567/scalar-generations-and-actual-reclaim";
    internal const string MapCase = "runtime/allocation567/shared-map-roots-custody";
    internal const string RollbackCase = "runtime/allocation567/failed-publication-not-garbage-collection";
    internal const string RetirementCase = "runtime/allocation567/isolation-and-held-retirement";
    internal const string HeapType = "Erlang.ProcessTermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Adopt = "AdoptGuestInto507";
    internal const string SetLocal = "SetLocalGraph507";
    internal const string Collect = "CollectGeneration508";
    internal const string Retire = "RetireAsync";
    internal const string Export = "Export";
    internal const string BeginConstruction = "BeginConstruction514";
    internal const string CreateFloat = "CreateFloat556";
    internal const string CreateInteger = "CreateInteger559";
    internal const string CreateReference = "CreateReference562";
    internal const string HeapGate = "Gate";
    internal const string AllocationSnapshot = "AllocationSnapshot567";
    internal const string Nursery = "NurseryAllocationWords";
    internal const string Survivor = "SurvivorAllocationWords";
    internal const string Old = "OldAllocationWords";
    internal const string Reclaimed = "ReclaimedAllocationWords";
    internal const string ReclaimedCollection = "ReclaimedAllocationWords567";
    internal const string PayloadSnapshot = "GenerationSnapshot508";
    internal const string NurseryPayload = "NurseryPayloadWords";
    internal const string SurvivorPayload = "SurvivorPayloadWords";
    internal const string OldPayload = "OldPayloadWords";
    internal const string ReclaimedPayload = "ReclaimedPayloadWords";
    internal const string PhysicalSnapshot = "PhysicalWordSnapshot526";
    internal const string Physical = "LivePhysicalWords";
    internal const string Headers = "LiveEncodedHeaderWords";
    internal const string Numeric = "LiveRawNumericWords556";
    internal const string First = "allocation567_first";
    internal const string Second = "allocation567_second";
    internal const string Failure = "Actual owned allocation accounting violated the generation/root/lifetime control.";
    internal const string CleanupFailure = "An allocation567 assertion and/or independent cleanup phase failed.";
    internal const string FrameCleanupFailure = "An allocation567 root frame failed cleanup.";
    internal const string HeapCleanupFailure = "An allocation567 heap failed retirement/drain.";
    internal const string RuntimeCleanupFailure = "An allocation567 runtime failed disposal.";
    internal const int DrainTimeoutSeconds = 10;
}

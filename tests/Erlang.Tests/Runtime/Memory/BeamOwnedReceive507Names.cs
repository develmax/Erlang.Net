namespace Erlang.Tests;

internal static class BeamOwnedReceive507Names
{
    internal const string ProtectedTest = "runtime/beam-owned507/receive/original-protected-receive-values-and-stacks";
    internal const string BeforeCallbackTest = "runtime/beam-owned507/receive/cancel-before-physical-callback-completion";
    internal const string AfterSelectionTest = "runtime/beam-owned507/receive/cancel-after-selection-before-admission";
    internal const string SourceRetirementTest = "runtime/beam-owned507/receive/source-message-after-sender-retirement";
    internal const string MetadataTest = "runtime/beam-owned507/receive/metadata-retains-payload-without-register-roots";
    internal const string RollbackTest = "runtime/beam-owned507/receive/removed-candidate-rollback";
    internal const string PendingTest = "runtime/beam-owned507/receive/pending-admission-cancellation";
    internal const string TraceFailureTest = "runtime/beam-owned507/receive/post-consumption-trace-failure-both-routes";
    internal const string Failure = "Owned BEAM receive507 contract failed.";
    internal const string CallbackEntered = "receive507/callback-entered";
    internal const string CallbackSelected = "receive507/callback-selected";
    internal const string GuestRun = "run";
    internal const string Chosen = "chosen507";
    internal const string Kill = "kill";
    internal const string Normal = "normal";
    internal const string WaitForExecution = "WaitForExecution";
    internal const string DeferredType = "Erlang.DeferredMailboxAdmission";
    internal const string SendDeferred = "SendDeferred";
    internal const string ClaimDeferred = "ClaimDeferred";
    internal const string Complete = "Complete";
    internal const string ReceiveBeam = "ReceiveBeamAsync";
    internal const string ReceiveGuard = "ReceiveBeamGuardAsync";
    internal const string MessageReceived = "MessageReceived";
    internal const string StateType = "Erlang.BeamRegisterState";
    internal const string SelectionType = "Erlang.BeamReceiveSelection";
    internal const string Write = "Write";
    internal const string Read = "Read";
    internal const string Clear = "ClearXExcept";
    internal const string ReadResource = "Read";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Words = "LivePayloadWords";
    internal const string Capacity = "CapacityWords";
    internal const string NativeArena = "OwnedWords504";
    internal const string NativeReserved = "ReservedWords";
    internal const string NativeRooted = "RootedWords";
    internal const string Collect = "Collect";
    internal const string CheckpointType = "Erlang.BeamOwnedCheckpoint507";
    internal const string Enter = "Enter";
    internal const string Unwound = "Unwound";
    internal const string TaskProperty = "Task";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}

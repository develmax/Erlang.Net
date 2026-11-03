namespace Erlang.Tests;

internal static class OwnedGuest506Names
{
    internal const string MessageTest = "runtime/owned-guest506/source-message-survives-sender-physical-retirement";
    internal const string SendEntry = "send_owned";
    internal const string MessageTag = "owned_message506";
    internal const string PrivateOption = "OwnedTermsPilot506";
    internal const string FactoryFailure = "The private guest-ownership diagnostic option is unavailable.";
    internal const string Test = "runtime/owned-guest506/compiled-payload-roots-dictionary-and-host-export";
    internal const string HeldTest = "runtime/owned-guest506/compiled-await-cancellation-and-source-closure-export";
    internal const string LifecycleInfoTest = "runtime/owned-guest506/process-info-dictionary-races-return-and-kill";
    internal const string LifecycleResume = "owned_guest_resume506";
    internal const string Undefined = "undefined";
    internal const string PhysicalTest = "runtime/owned-guest506/logical-down-before-held-host-root-and-physical-dispose-join";
    internal const string CurrentInvocation = "CurrentOwned506";
    internal const string Arguments = "Arguments";
    internal const string Retirement = "OwnedTermRetirement506";
    internal const string ErlangModule = "erlang";
    internal const string ProcessInfo = "process_info";
    internal const string DictionaryItem = "dictionary";
    internal const string ExternalNode = "owned506-peer@loopback";
    internal const string OwnersClosingField = "ownedTermOwnersClosing506";
    internal const string ErrorsTest = "runtime/owned-guest506/ordinary-and-owned-source-errors-and-full-stacks";
    internal const string FailureEntry = "failure";
    internal const string CapturedTaskTest = "runtime/owned-guest506/captured-context-public-entry-reopens-independent-roots";
    internal const string DictionaryTest = "runtime/owned-guest506/concurrent-host-dictionary-writes-and-real-collection";
    internal const string DescriptorTest = "runtime/owned-guest506/blocked-source-descriptor-does-not-hold-heap-gate";
    internal const string Capture = "capture";
    internal const string Echo = "echo";
    internal const string Gate = "gate";
    internal const string Roots = "RootFrames";
    internal const string GuestModule = "owned_guest506";
    internal const string ProbeModule = "owned_probe506";
    internal const string Live = "live";
    internal const string Held = "held";
    internal const string Run = "run";
    internal const string Make = "make";
    internal const string Key = "key";
    internal const string Kill = "kill";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Words = "LivePayloadWords";
    internal const string Collect = "Collect";
    internal const string Capacity = "CapacityWords";
    internal const string Storage = "OwnedStorage506";
    internal const string Unwound = "Unwound";
    internal const string TaskProperty = "Task";
    internal const string Failure = "Owned guest506 integration contract failed.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(owned_guest506).
        -export([run/1, held/1, make/1, capture/1, failure/1, send_owned/2]).
        run(X) -> A = {X, [1,2]}, owned_probe506:live(A), put(key,A), get(key).
        held(X) -> A = {X, [1,2]}, owned_probe506:held(A), A.
        make(X) -> A = {X, [1,2]}, fun() -> A end.
        capture(X) -> A = {X, [1,2]}, owned_probe506:capture(A), A.
        failure(X) -> erlang:error({X,[1,2]}).
        send_owned(P,X) -> P ! {owned_message506,X}, ok.
        """;
}

namespace Erlang.NifNativeBackend;

public static class SameHeapMailboxCaseNames
{
    public const string Physical = "runtime/statistics-self-on-heap-send-real-same-graph-no-pin-no-refcount-inflation";
    public const string Capability = "runtime/self-message-canonical-exact-delegate-and-public-replay-refusal";
    internal const string Ok = "ok";
    internal const string Module = "same_heap_mailbox_physical";
    internal const string Probe = "same_heap_mailbox_probe";
    internal const string Checkpoint = "checkpoint";
    internal const string File = "same_heap_mailbox_physical.erl";
    internal const string Run = "run";
    internal const string Operator = "operator";
    internal const string Bif = "bif";
    internal const string Functions = "functions";
    internal const string Gate = "gate";
    internal const string Selection = "SelectedOwnedBinding507";
    internal const string Admitted = "LocalMessage";
    internal const string Opaque = "Opaque";
    internal const string Failure = "Actual self on_heap send copied or pinned its sender graph, inflated the writable Binary refcount, or accepted public delegate authority.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    internal const string Source = """
-module(same_heap_mailbox_physical).
-export([run/1,append/2]).
run(Mode) ->
    process_flag(message_queue_data,on_heap),
    A = append(<<>>,7),
    B = <<A/binary,0:2048>>,
    true = erlang:garbage_collect(),
    true = erlang:garbage_collect(),
    Before = binary:referenced_byte_size(B),
    same_heap_mailbox_probe:checkpoint(1),
    case Mode of
        operator -> self() ! {self_heap,B};
        bif -> erlang:send(self(),{self_heap,B})
    end,
    same_heap_mailbox_probe:checkpoint(2),
    receive {self_heap,C} ->
        true = B =:= C,
        After = binary:referenced_byte_size(B),
        same_heap_mailbox_probe:checkpoint(3),
        Next = append(B,9),
        {Before,After,binary:referenced_byte_size(Next),C,Next}
    end.
append(B,N) -> <<B/binary,N>>.
""";
}

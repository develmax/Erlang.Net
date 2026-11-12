-module(saved_call_result_probe).
-export([retain/2, reuse/2, overlap/2, both/2, constructed/2]).

retain(A, B) ->
    Value = pack(A, B),
    touch(B),
    echo(Value).

reuse(A, B) ->
    First = pack(A, B),
    touch(B),
    Second = echo(First),
    touch(B),
    echo(Second).

overlap(A, B) ->
    First = pack(A, B),
    Second = pack(B, A),
    touch(B),
    pack(First, Second).

both(A, B) ->
    Value = pack(A, B),
    touch(Value),
    echo(Value).

constructed(A, B) ->
    Value = pack(A, B),
    touch([Value|B]),
    pack(Value, B).

pack(A, B) -> {A,B}.
echo(Value) -> Value.
touch(Key) -> erlang:put(saved_call_result_probe, Key).

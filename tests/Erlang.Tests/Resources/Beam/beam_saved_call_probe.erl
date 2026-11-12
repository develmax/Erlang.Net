-module(beam_saved_call_probe).
-export([retain/2, reuse/2, overlap/2]).

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

pack(A, B) -> {A,B}.
echo(Value) -> Value.
touch(Key) -> persistent_term:put(beam_saved_call_probe, Key).

-module(beam_bif_tail_sequence).
-export([run/2, keep/1]).

run(A, B) ->
    _First = stash(first, A),
    stash(second, B),
    pair(get(first), get(second)).

keep(Key) ->
    Value = lookup_key(Key),
    pair(Value, get(Value)).

stash(Key, Value) -> put(Key, Value), Value.
lookup_key(Key) -> Key.
pair(A, B) -> {A, B}.

-module(beam_list_call_sequence_probe).
-export([forward/2, retain/2]).

forward([Head|Tail], Key) ->
    Items = get_state(Head),
    Rest = lists:keydelete(Key, 1, Items),
    update_state(Head, Rest),
    Done = get_state(done),
    update_state(done, [Tail|Done]);
forward([], _) ->
    empty.

retain([Head|Tail], Key) ->
    Value = pack(Head, Key),
    touch(Tail),
    echo(Value);
retain([], _) ->
    empty.

get_state(Key) -> persistent_term:get(Key).
update_state(Key, Value) -> persistent_term:put(Key, Value).
pack(A, B) -> {A, B}.
echo(Value) -> Value.
touch(Value) -> persistent_term:put(touched, Value).

-module(straight_call_sequence_probe).
-export([run/1, state/0, carry/2, nonadjacent/2]).

run(Name) ->
    Pending = get_state(pending),
    Remaining = lists:keydelete(Name, 1, Pending),
    update_state(pending, Remaining),
    Done = get_state(done),
    update_state(done, [Name|Done]).

state() ->
    {get_state(pending), get_state(done)}.

carry(A, B) ->
    Pair = pack(A, B),
    update_state(saved, [Pair|B]),
    echo(A).

nonadjacent(A, B) ->
    Pair = pack(A, B),
    update_state(nonadjacent, B),
    echo(Pair).

get_state(Key) ->
    erlang:get(Key).

update_state(Key, Value) ->
    erlang:put(Key, Value).

pack(A, B) ->
    {A, B}.

echo(Value) ->
    Value.

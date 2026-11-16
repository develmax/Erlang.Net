-module(beam_straight_call_probe).
-export([run/1]).

run(Name) ->
    Pending = get_state(pending),
    Remaining = lists:keydelete(Name, 1, Pending),
    update_state(pending, Remaining),
    Done = get_state(done),
    update_state(done, [Name|Done]).

get_state(Key) ->
    persistent_term:get(Key).

update_state(Key, Value) ->
    persistent_term:put(Key, Value).

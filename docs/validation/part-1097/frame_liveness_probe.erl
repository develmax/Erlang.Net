-module(frame_liveness_probe).
-export([chain/3, tail/3, alias_live/3, nested/3, reuse/3, reuse_tail/3]).
chain(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    Middle = erlang:put(middle, Old),
    Last = erlang:put(last, Middle),
    {Last, Keep}.
tail(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    Middle = erlang:put(middle, Old),
    pair(Middle, Keep).
alias_live(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    Alias = Old,
    Middle = erlang:put(middle, Old),
    {Alias, Middle, Keep}.
nested(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    Middle = erlang:put(middle, Old),
    case Keep of
        old -> {Old, Middle};
        _ -> {Middle, Keep}
    end.
pair(A, B) -> {A, B}.

reuse(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    erlang:put(mark, Keep),
    Middle = erlang:put(middle, Old),
    erlang:put(mark2, Keep),
    {Middle, Keep}.
reuse_tail(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    erlang:put(mark, Keep),
    Middle = erlang:put(middle, Old),
    pair(Middle, Keep).

-module(entry_operand_probe).
-export([pair/2, with_literal/2, remote_tail/2, retain/3]).

pair(A, B) ->
    pair_result(record(first, A), {tag, record(second, B)}).

with_literal(A, B) ->
    triple_result(marker, record(first, A), {tag, record(second, B)}).

remote_tail(A, B) ->
    lists:append(record(first, A), [record(second, B)]).

retain(A, B, Keep) ->
    triple_result(Keep, record(first, A), {tag, record(second, B), Keep}).

record(Key, Value) ->
    put(trace, [Key | get(trace)]),
    case Value of
        fail -> error(Key);
        _ -> Value
    end.

pair_result(A, B) ->
    {A, B}.

triple_result(A, B, C) ->
    {A, B, C}.

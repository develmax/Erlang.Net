-module(beam_tuple_field_list_helper).
-export([project/1, raw/1, strict/1, repeated/1, with_effect/1, caught/2]).
-record(bucket,{items,id}).

project(Bucket) -> [Value || {_, Value} <- Bucket#bucket.items].
raw(Values) -> [Value || {_, Value} <- Values].
strict(Values) -> [Value || {_, Value} <:- Values].
repeated(Values) -> [Value || {Value, Value} <- Values].
with_effect(Values) -> [observe(Value) || {_, Value} <- Values].

observe(Value) ->
    put(trace, [Value|get(trace)]),
    case Value of
        stop -> erlang:error(selected);
        _ -> Value
    end.

caught(project, Value) ->
    try project(Value)
    catch error:Reason -> Reason
    end;
caught(strict, Value) ->
    try strict(Value)
    catch error:Reason -> Reason
    end;
caught(effect, Value) ->
    put(trace, []),
    try with_effect(Value)
    catch error:Reason -> {Reason, get(trace)}
    end.

-module(computed_case_tuple_probe).
-export([inline/2, bound/3, expired/2, projected/2]).

inline(Mode, Value) ->
    case read(Mode) of
        active -> {wrapped, box(Value)};
        stopped -> Value
    end.

bound(Mode, Value, Context) ->
    case read(Mode) of
        active ->
            Result = box(Value),
            {wrapped, Context, Result, Result};
        stopped ->
            {stopped, Context}
    end.

expired(Mode, Value) ->
    case read(Mode) of
        active -> {wrapped, box(Value), Value};
        stopped -> stopped
    end.

projected(Key, Map) ->
    case maps:find(Key, Map) of
        {ok, Value} -> {wrapped, box(Value)};
        _ -> missing
    end.

read(Value) ->
    erlang:put(last_selector, Value),
    Value.

box(Value) ->
    erlang:put(last_value, Value),
    {boxed, Value}.

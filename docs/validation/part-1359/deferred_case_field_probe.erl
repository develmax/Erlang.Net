-module(deferred_case_field_probe).
-export([deferred/3, mixed/2, projected/1, nested/2]).

deferred(Mode, Value, Context) ->
    case read(Mode) of
        {ready, Payload} -> {Payload, box(Value)};
        _ -> {fallback, Context}
    end.

mixed(Mode, Value) ->
    case read(Mode) of
        {ready, Payload} -> {Payload, box(Value)};
        stopped -> {stopped, Value}
    end.

projected(Mode) ->
    case read(Mode) of
        {ready, Payload} -> {ready, box(Payload)}
    end.

read(Value) ->
    erlang:put(last_selector, Value),
    Value.

box(Value) ->
    erlang:put(last_value, Value),
    {boxed, Value}.

nested(Mode, Value) ->
    case read(Mode) of
        {ready, {_, Payload}} -> {Payload, box(Value)};
        stopped -> {stopped, Value}
    end.


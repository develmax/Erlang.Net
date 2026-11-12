-module(beam_entry_tuple_probe).
-export([run/3, keyfetch/2, forward/3, forward_pair/2, rebuild/3, nested/2, nested_dead/2]).
run({tag, {Value}} = Tagged, {Other}, Options)
  when is_list(Options), is_tuple(Tagged), is_integer(Value) ->
    {Tagged, erlang:integer_to_binary(Value), Other}.
keyfetch(Key, Entries) ->
    {Key,Value} = lists:keyfind(Key, 1, Entries),
    Value.
forward(Key, Entries, Saved) ->
    {Key,A,B} = lists:keyfind(Key, 1, Entries),
    finish(A,Saved,B).
forward_pair(Key, Entries) ->
    {Key,A,B} = lists:keyfind(Key, 1, Entries),
    finish_pair(A,B).
rebuild(Key, Entries, Saved) ->
    {Key,A,B} = lists:keyfind(Key, 1, Entries),
    {B,Saved,A}.
nested(Key, Entries) ->
    {Key,Value} = lists:keyfind(Key, 1, identity(Entries)),
    Value.
nested_dead(Key, Entries) ->
    {_,Value} = lists:keyfind(Key, 1, identity(Entries)),
    Value.
identity(Entries) -> Entries.
finish(A,S,B) -> {A,S,B}.
finish_pair(A,B) -> {A,B}.


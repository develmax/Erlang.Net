-module(beam_entry_tuple_probe).
-export([run/3, keyfetch/2]).
run({tag, {Value}} = Tagged, {Other}, Options)
  when is_list(Options), is_tuple(Tagged), is_integer(Value) ->
    {Tagged, erlang:integer_to_binary(Value), Other}.
keyfetch(Key, Entries) ->
    {Key,Value} = lists:keyfind(Key, 1, Entries),
    Value.


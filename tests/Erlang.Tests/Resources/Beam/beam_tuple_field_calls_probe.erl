-module(beam_tuple_field_calls_probe).
-export([pair/1, nested/1, ordered/1]).

pair(Values) ->
    Sorted = lists:sort(Values),
    {lists:reverse(Sorted), lists:sublist(Sorted, 2)}.

nested(Values) ->
    {tag, [{sorted, lists:sort(lists:reverse(Values))}]}.

ordered(Key) ->
    {erlang:put(Key, first), erlang:put(Key, second)}.

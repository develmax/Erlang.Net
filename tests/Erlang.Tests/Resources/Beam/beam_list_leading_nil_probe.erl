-module(beam_list_leading_nil_probe).
-export([find/2]).

find(_, []) ->
    missing;
find(Key, [{Value,Key}|_]) ->
    Value;
find(Key, [_|Rest]) ->
    find(Key, Rest).

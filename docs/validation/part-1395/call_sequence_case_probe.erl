-module(call_sequence_case_probe).
-export([options/2, tag/2, ordered/1]).

options(File, Options) ->
    Initial = add(base, Options),
    Prepared = add(filename:dirname(File), Initial),
    case proplists:get_value(outdir, Options) of
        undefined -> Prepared;
        Dir -> add(Dir, Prepared)
    end.

add(Dir, Options) ->
    [{i,Dir}|Options].

tag(Items, Flag) ->
    Reversed = lists:reverse(Items),
    Found = lists:member(Flag, Reversed),
    case Found of
        true -> {yes,Reversed};
        false -> {no,Items}
    end.

ordered(Key) ->
    First = mark(Key, first),
    Values = pair(mark(Key, second), mark(Key, third)),
    case lists:member(First, Values) of
        true -> unexpected;
        false -> Values
    end.

pair(A, B) ->
    [A,B].

mark(Key, Value) ->
    erlang:put(Key, [Value|erlang:get(Key)]),
    Value.

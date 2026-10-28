-module(map_postreturn505).
-export([load/1, make/3, run_case/3, run/0]).

load(Path) -> erlang:load_nif(Path, []).

make(_, _, _) -> erlang:nif_error(not_loaded).

lookup(Key, Map) ->
    case maps:find(Key, Map) of
        {ok, Value} -> {1, Value};
        error -> {0, -1}
    end.

physical_pairs(Map) ->
    [{binary:first(Key), Value} || {Key, Value} <- maps:to_list(Map)].

iterate(Iterator) ->
    case maps:next(Iterator) of
        none -> [];
        {Key, Value, Next} -> [{binary:first(Key), Value} | iterate(Next)]
    end.

run_case(Count, Bytes, Target) ->
    {Map, Current, Previous} = make(Count, Bytes, Target),
    Before = {map_size(Map), lookup(Current, Map), lookup(Previous, Map),
              physical_pairs(Map), iterate(maps:iterator(Map))},
    Fresh = maps:from_list(maps:to_list(Map)),
    Exact = {Map =:= Fresh, {Map} =:= {Fresh}, [Map] =:= [Fresh]},
    % Inner map hashing must consume the saved physical shape, even when the map
    % becomes a key inside a larger HAMT after the originating NIF has returned.
    Outer = maps:from_list([{I, I} || I <- lists:seq(1, 32)] ++ [{Map, kept}]),
    Nested = {maps:find(Map, Outer), maps:find(Fresh, Outer)},
    erlang:garbage_collect(),
    After = {map_size(Map), lookup(Current, Map), lookup(Previous, Map),
             physical_pairs(Map), iterate(maps:iterator(Map))},
    {Before, Exact, Nested, After}.

run() ->
    [{N, Bytes, Target, run_case(N, Bytes, Target)} ||
        N <- [2, 32, 33, 65], Bytes <- [64, 65], Target <- [1, 128, 129, 143]].

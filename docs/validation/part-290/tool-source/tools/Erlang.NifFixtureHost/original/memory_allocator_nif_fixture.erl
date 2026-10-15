-module(memory_allocator_nif_fixture).
-export([load/2, increment/1, greeting/0]).
-nifs([increment/1, greeting/0]).

load(Path, LoadInfo) ->
    erlang:load_nif(Path, LoadInfo).

increment(_) ->
    erlang:nif_error(not_loaded).

greeting() ->
    erlang:nif_error(not_loaded).

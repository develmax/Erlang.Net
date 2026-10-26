-module(map_creation501).
-export([load/1, empty/0, observe/0, run/0]).
load(Path) -> erlang:load_nif(Path, []).
empty() -> erlang:nif_error(not_loaded).
observe() -> erlang:nif_error(not_loaded).
run() ->
    A = empty(),
    B = empty(),
    {A, B, A =:= B, is_map(A), map_size(A), observe()}.

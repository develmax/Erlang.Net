-module(nif_resource_fixture).
-export([load/1, new/0, inspect/1, keep/1, release/0, stats/0, native_only/0, run/0]).
-nifs([new/0, inspect/1, keep/1, release/0, stats/0, native_only/0]).
load(Path) -> erlang:load_nif(Path, 0).
new() -> erlang:nif_error(not_loaded).
inspect(_) -> erlang:nif_error(not_loaded).
keep(_) -> erlang:nif_error(not_loaded).
release() -> erlang:nif_error(not_loaded).
stats() -> erlang:nif_error(not_loaded).
native_only() -> erlang:nif_error(not_loaded).
run() ->
    {Resource, Binary, Value} = new(),
    {Value, 16} = inspect(Resource),
    ok = keep(Resource),
    {Value, 16, binary_to_list(Binary)}.

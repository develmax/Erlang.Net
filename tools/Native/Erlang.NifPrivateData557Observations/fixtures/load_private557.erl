-module(load_private557).
-export([load/1, read/0]).
load(Path) -> erlang:load_nif(Path, []).
read() -> erlang:nif_error(not_loaded557).

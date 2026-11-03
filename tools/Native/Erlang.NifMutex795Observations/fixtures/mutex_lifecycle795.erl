-module(mutex_lifecycle795).
-export([load/1,probe/0,destroy/0,marker/0]).
-nifs([probe/0,destroy/0]).
-ifndef(BEAM_VERSION795).
-define(BEAM_VERSION795,1).
-endif.
load(Path) -> erlang:load_nif(Path,0).
probe() -> erlang:nif_error(not_loaded).
destroy() -> erlang:nif_error(not_loaded).
marker() -> ?BEAM_VERSION795.

-module(init_subject582).
-export([load/1,status/0,marker/0,make/0]).
-ifndef(BEAM_GENERATION582).
-define(BEAM_GENERATION582,1).
-endif.
load(Path)->erlang:load_nif(Path,0).
status()->erlang:nif_error(not_loaded).
marker()->?BEAM_GENERATION582.
make()->fun status/0.

-module(snprintf791_oracle).
-export([load/1,run/0]).
load(Path) -> erlang:load_nif(Path,0).
run() -> erlang:nif_error(nif_not_loaded).

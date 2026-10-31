-module(mutex795).
-export([load/1,test/0]).
load(Path) -> erlang:load_nif(Path,0).
test() -> erlang:nif_error(nif_not_loaded).

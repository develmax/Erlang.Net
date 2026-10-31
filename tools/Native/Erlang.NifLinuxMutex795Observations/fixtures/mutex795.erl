-module(mutex795).
-export([load/1,test/0,hold/2]).
-nifs([test/0,hold/2]).
load(Path) -> erlang:load_nif(Path,0).
test() -> erlang:nif_error(not_loaded).
hold(_,_) -> erlang:nif_error(not_loaded).

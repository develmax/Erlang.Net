%% coding: utf-8
-module('nif_ascii690').
-export([load/1,identity/0,second/1]).
-nifs([identity/0,second/1]).
load(Path) -> erlang:load_nif(Path, 0).
identity() -> erlang:nif_error(not_loaded).
second(_) -> erlang:nif_error(not_loaded).

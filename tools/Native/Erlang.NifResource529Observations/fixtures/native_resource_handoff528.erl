-module(native_resource_handoff528).
-export([load/2, make528/0, inspect528/1, counts528/0, probe528/1]).
load(Path, UnloadMarker) -> erlang:load_nif(Path, UnloadMarker).
make528() -> erlang:nif_error(nif_not_loaded).
inspect528(_) -> erlang:nif_error(nif_not_loaded).
counts528() -> erlang:nif_error(nif_not_loaded).

probe528(_) -> erlang:nif_error(nif_not_loaded).

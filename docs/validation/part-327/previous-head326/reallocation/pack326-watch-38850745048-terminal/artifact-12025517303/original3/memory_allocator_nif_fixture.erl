-module(memory_allocator_nif_fixture).
-export([load/1, increment/1, greeting/0]).
-nifs([increment/1, greeting/0]).
load(Path) -> erlang:load_nif(Path, 42).
increment(_) -> erlang:nif_error(reallocation_probe_not_loaded).
greeting() -> erlang:nif_error(reallocation_probe_not_loaded).

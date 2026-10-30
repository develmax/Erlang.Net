-module(nif_env709).
-export([load/2, roundtrip/1, counts/0, allocator_stable/0, foreign_thread/0, capture/0, marker/0]).
-ifndef(CODE_VERSION709).
-define(CODE_VERSION709, 1).
-endif.

load(Path, Info) -> erlang:load_nif(Path, Info).
roundtrip(_) -> erlang:nif_error(not_loaded).
counts() -> erlang:nif_error(not_loaded).
allocator_stable() -> erlang:nif_error(not_loaded).
foreign_thread() -> erlang:nif_error(not_loaded).
capture() -> {fun roundtrip/1, fun counts/0, fun allocator_stable/0, fun foreign_thread/0}.
marker() -> ?CODE_VERSION709.

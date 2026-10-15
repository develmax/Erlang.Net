-module(nif_pending_varargs_fixture).
-export([load/2, tuple_empty/0, tuple_single/0, tuple_spill/0, list_empty/0, list_single/0, list_spill/0, pending_none/0, pending_raise_twice/0, pending_badarg_ignored/0, pending_detail/0, pending_badarg_then_raise/0, pending_raise_then_badarg/0]).
-nifs([tuple_empty/0, tuple_single/0, tuple_spill/0, list_empty/0, list_single/0, list_spill/0, pending_none/0, pending_raise_twice/0, pending_badarg_ignored/0, pending_detail/0, pending_badarg_then_raise/0, pending_raise_then_badarg/0]).

load(Path, Info) -> erlang:load_nif(Path, Info).

tuple_empty() -> erlang:nif_error(not_loaded).
tuple_single() -> erlang:nif_error(not_loaded).
tuple_spill() -> erlang:nif_error(not_loaded).
list_empty() -> erlang:nif_error(not_loaded).
list_single() -> erlang:nif_error(not_loaded).
list_spill() -> erlang:nif_error(not_loaded).
pending_none() -> erlang:nif_error(not_loaded).
pending_raise_twice() -> erlang:nif_error(not_loaded).
pending_badarg_ignored() -> erlang:nif_error(not_loaded).
pending_detail() -> erlang:nif_error(not_loaded).
pending_badarg_then_raise() -> erlang:nif_error(not_loaded).
pending_raise_then_badarg() -> erlang:nif_error(not_loaded).

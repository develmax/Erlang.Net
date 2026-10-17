-module(term_callback_contract).
-export([load/1,inspect/1,inspect_null/1,arrays/1]).

load(Path) -> erlang:load_nif(Path, []).

inspect(_) -> erlang:nif_error(not_loaded).

inspect_null(_) -> erlang:nif_error(not_loaded).

arrays(_) -> erlang:nif_error(not_loaded).

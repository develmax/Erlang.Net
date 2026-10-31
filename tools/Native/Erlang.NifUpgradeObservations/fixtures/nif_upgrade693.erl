-module(nif_upgrade693).
-export([load/2, identity/0, state/0, resource/0, make/0, marker/0]).
-nifs([identity/0,state/0,resource/0]).
-ifndef(CODE_VERSION693).
-define(CODE_VERSION693, 1).
-endif.

load(Path, Info) -> erlang:load_nif(Path, Info).
identity() -> erlang:nif_error(not_loaded).
state() -> erlang:nif_error(not_loaded).
resource() -> erlang:nif_error(not_loaded).
make() -> {fun identity/0, fun state/0}.
marker() -> ?CODE_VERSION693.

-module(erts_debug).
-export([identity/1, call_opaque/1, capture/1, copy_shared/2]).
identity(T) -> T.
call_opaque(T) -> copy_shared(T, false).
capture(T) -> fun() -> copy_shared(T, false) end.
copy_shared(_, _) -> erlang:nif_error(undef).

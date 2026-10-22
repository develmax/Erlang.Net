-module(erts_internal).
-export([term_to_string/2,no_aux_work_threads/0]).
term_to_string(_Term, _Limit) ->
    erlang:nif_error(undef).
no_aux_work_threads() ->
    erlang:nif_error(undef).

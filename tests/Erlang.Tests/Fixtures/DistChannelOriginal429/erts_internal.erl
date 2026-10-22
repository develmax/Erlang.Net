-module(erts_internal).
-export([get_dflags/0,new_connection/1,create_dist_channel/3]).

get_dflags() ->
    erlang:nif_error(undefined).

new_connection(_Node) ->
    erlang:nif_error(undefined).

create_dist_channel(_Node, _DistCtrlr, _Tpl) ->
    erlang:nif_error(undefined).

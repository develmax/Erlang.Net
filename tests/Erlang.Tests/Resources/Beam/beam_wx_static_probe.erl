-module(beam_wx_static_probe).
-export([get/0, set/1, notify/0, new/1]).
-record(wx_env,{ref, sv, debug=0}).
-define(ENV, case erlang:get(wx_env) of
                 #wx_env{ref=Ref} -> Ref;
                 _ -> erlang:error({wx,unknown_env})
             end).

get() ->
    wxe_util:queue_cmd(?ENV,101),
    wxe_util:rec(101).

set(Value) when is_boolean(Value) ->
    wxe_util:queue_cmd(Value,?ENV,102),
    ok.

notify() ->
    wxe_util:queue_cmd(?ENV,103).

new(Text) when is_list(Text) orelse is_binary(Text) ->
    Binary = unicode:characters_to_binary(Text),
    wxe_util:queue_cmd(Binary,?ENV,104),
    wxe_util:rec(104).
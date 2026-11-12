-module(wx_option_queue_probe).
-export([post/1, request/1]).
post(Options) when is_list(Options) ->
    Mapper = fun({text, Value}) -> Converted = unicode:characters_to_binary(Value), {text, Converted};
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 575).
request(Options) when is_list(Options) ->
    Mapper = fun({text, Value}) -> Converted = unicode:characters_to_binary(Value), {text, Converted};
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 576),
    wxe_util:rec(576).

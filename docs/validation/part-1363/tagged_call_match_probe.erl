-module(tagged_call_match_probe).
-export([direct/1, retained/1, external/1, payload/0, mapped/2, mapped_post/2]).

direct(Input) ->
    {ready, _} = check(),
    finish(Input).

retained(Input) ->
    Produced = produce(Input),
    {ready, _} = check(),
    finish(Produced).

external(Input) ->
    {ready, _} = check(),
    lists:reverse(Input).

payload() ->
    {ready, Value} = check(),
    Value.

mapped({wx_ref, _, Type, _} = Parent, Options) when is_list(Options) ->
    Type =:= wxWindow orelse Type:parent_class(wxWindow),
    Mapper = fun({value, _} = Option) -> Option;
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    {ready, _} = check(),
    wxe_util:queue_cmd(Parent, Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 906),
    wxe_util:rec(906).

mapped_post({wx_ref, _, Type, _} = Parent, Options) when is_list(Options) ->
    Type =:= wxWindow orelse Type:parent_class(wxWindow),
    Mapper = fun({value, _} = Option) -> Option;
                (Bad) -> erlang:error({badoption, Bad})
             end,
    Mapped = lists:map(Mapper, Options),
    {ready, _} = check(),
    wxe_util:queue_cmd(Parent, Mapped, case erlang:get(wx_env) of
        {wx_env, Ref, _, _} -> Ref;
        _ -> erlang:error({wx, unknown_env})
    end, 907).

check() ->
    put(match_probe_checked, yes),
    get(match_probe_value).

produce(Input) ->
    put(match_probe_produced, Input),
    {produced, Input}.

finish(Input) ->
    put(match_probe_finished, Input),
    {finished, Input}.

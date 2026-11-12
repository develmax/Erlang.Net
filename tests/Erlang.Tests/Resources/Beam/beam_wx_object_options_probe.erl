-module(beam_wx_object_options_probe).
-export([run/3]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(CLASS(Type, Class), (Type =:= Class) orelse Type:parent_class(Class)).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).
run(#wx_ref{type=FirstType}=First, #wx_ref{type=SecondType}=Second, Options)
  when is_list(Options) ->
    ?CLASS(FirstType, wxWindow),
    ?CLASS(SecondType, wxAuiManager),
    Mapper = fun({level, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(First, Second, Mapped, ?ENV, 2518),
    wxe_util:rec(2518).
